using System.Diagnostics;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mms.Application.Shareholders.Commands;
using Mms.Application.Shareholders.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Parsing;
using Mms.IntegrationTests.Fixtures;
using Mms.UnitTests.Helpers;
using Xunit.Abstractions;

namespace Mms.IntegrationTests.Tests;

public class ImportFlowIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ImportFlowIntegrationTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private async Task<Guid> SeedMeetingAsync()
    {
        var db = _fixture.CreateFreshDbContext();

        // Create company first
        var companyId = Guid.NewGuid();
        var existingCompany = await db.Companies.FirstOrDefaultAsync();
        if (existingCompany != null)
        {
            companyId = existingCompany.Id;
        }
        else
        {
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Công ty CP Import Test",
                TaxCode = "9876543210",
                LegalRepName = "Lê Văn A",
                LegalRepTitle = "TGĐ",
                CharterCapital = 500_000_000_000,
                TotalSharesIssued = 50_000_000,
                TotalVotingShares = 50_000_000,
            });
            await db.SaveChangesAsync();
        }

        // Create meeting
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = $"Import Test Meeting {Guid.NewGuid():N}",
            MeetingType = MeetingType.Annual,
            MeetingDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            RecordDate = DateOnly.FromDateTime(DateTime.Today),
            TotalVotingShares = 50_000_000,
            Status = MeetingStatus.New,
        };
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();
        return meeting.Id;
    }

    private List<ShareholderImportDto> ParseXlsx(MemoryStream xlsx)
    {
        var parser = new VsdcParser();
        var result = parser.Parse(xlsx);
        return result.Rows
            .Select((r, i) => VsdcRowMapper.Map(r, i + 1))
            .ToList();
    }

    // TC-01: PERFORMANCE — Import 1000 rows < 10 seconds
    [Fact]
    public async Task ImportThousandRows_CompletesUnder10Seconds()
    {
        var meetingId = await SeedMeetingAsync();
        using var xlsx = VsdcXlsxBuilder.BuildRows(1000);
        var dtos = ParseXlsx(xlsx);

        var mediator = _fixture.CreateMediator();

        var sw = Stopwatch.StartNew();
        var result = await mediator.Send(new ImportShareholdersCommand(meetingId, dtos));
        sw.Stop();

        _output.WriteLine($"⚡ Import 1000 rows: {sw.ElapsedMilliseconds}ms");

        result.TotalRead.Should().Be(1000);
        result.Inserted.Should().Be(1000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "Performance gate: 1000 rows must import in under 10 seconds");

        // Verify DB
        var db = _fixture.CreateFreshDbContext();
        var count = await db.Shareholders
            .Where(s => s.MeetingId == meetingId)
            .CountAsync();
        count.Should().Be(1000);
    }

    // TC-02: WIPE-AND-RELOAD — Import same file twice → 1000 rows in DB (not 2000)
    [Fact]
    public async Task ImportSameFileTwice_WipeAndReload_ExactCount()
    {
        var meetingId = await SeedMeetingAsync();
        using var xlsx1 = VsdcXlsxBuilder.BuildRows(100);
        var dtos = ParseXlsx(xlsx1);

        var mediator = _fixture.CreateMediator();

        // First import
        var r1 = await mediator.Send(new ImportShareholdersCommand(meetingId, dtos));
        r1.Inserted.Should().Be(100);

        // Second import — same data → should DELETE + INSERT (Wipe-and-Reload)
        var r2 = await mediator.Send(new ImportShareholdersCommand(meetingId, dtos));
        r2.Inserted.Should().Be(100);

        // DB should have exactly 100 (not 200)
        var db = _fixture.CreateFreshDbContext();
        var count = await db.Shareholders
            .Where(s => s.MeetingId == meetingId)
            .CountAsync();
        count.Should().Be(100);
    }

    // TC-03: Import with non-existent meeting ID → FK violation → rollback (0 rows in DB)
    [Fact]
    public async Task ImportWithInvalidMeeting_ThrowsAndRollsBack()
    {
        var fakeMeetingId = Guid.NewGuid(); // Non-existent → FK violation
        var dtos = new List<ShareholderImportDto>
        {
            new()
            {
                RowIndex = 1, DisplayOrder = 1, VsdcRow = "1.1",
                FullName = "Test", IdNumber = "CMT001",
                VotingRights = 100,
            }
        };

        var mediator = _fixture.CreateMediator();

        Func<Task> act = () => mediator.Send(
            new ImportShareholdersCommand(fakeMeetingId, dtos));

        await act.Should().ThrowAsync<Exception>();

        // Verify no partial data leaked
        var db = _fixture.CreateFreshDbContext();
        var count = await db.Shareholders
            .Where(s => s.MeetingId == fakeMeetingId)
            .CountAsync();
        count.Should().Be(0, "transaction should have rolled back");
    }
}
