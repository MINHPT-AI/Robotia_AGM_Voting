using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mms.Application.Meetings.Commands;
using Mms.Application.Meetings.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.IntegrationTests.Fixtures;

namespace Mms.IntegrationTests.Tests;

public class MeetingCrudIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public MeetingCrudIntegrationTests(DatabaseFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Seeds a Company so meetings have a valid FK.
    /// Returns the company ID.
    /// </summary>
    private async Task<Guid> SeedCompanyAsync()
    {
        var db = _fixture.CreateFreshDbContext();
        var existing = await db.Companies.FirstOrDefaultAsync();
        if (existing != null) return existing.Id;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Công ty CP Test",
            TaxCode = "0123456789",
            LegalRepName = "Nguyễn Văn Test",
            LegalRepTitle = "Giám đốc",
            CharterCapital = 100_000_000_000,
            TotalSharesIssued = 10_000_000,
            TotalVotingShares = 10_000_000,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }

    private async Task<CreateMeetingCommand> MakeCreateCommand()
    {
        var companyId = await SeedCompanyAsync();
        return new CreateMeetingCommand(
            CompanyId: companyId,
            Title: "ĐHĐCĐ 2025",
            MeetingType: MeetingType.Annual,
            MeetingDate: DateTime.UtcNow.AddDays(30),
            Location: "Khách sạn Rex",
            RecordDate: DateOnly.FromDateTime(DateTime.Today),
            TotalVotingShares: 10_000_000,
            Chairman: "Ông A",
            Secretary: "Bà B",
            Notes: null,
            Resolutions: new List<ResolutionDto>
            {
                new(null, 1, "Thông qua BCTC", "Nội dung TQ 1"),
                new(null, 2, "Phân phối lợi nhuận", "Nội dung TQ 2"),
            },
            Candidates: new List<CandidateDto>
            {
                new(null, 1, "Nguyễn Văn C", "HĐQT", "GĐ", 1980, null),
                new(null, 2, "Trần Thị D", "BKS", null, 1985, null),
            });
    }

    // TC-01: Create meeting with resolutions + candidates → persists all
    [Fact]
    public async Task CreateMeeting_WithResolutionsAndCandidates_PersistsAll()
    {
        var mediator = _fixture.CreateMediator();
        var cmd = await MakeCreateCommand();

        var meetingId = await mediator.Send(cmd);

        meetingId.Should().NotBeEmpty();

        var db = _fixture.CreateFreshDbContext();
        var meeting = await db.Meetings
            .Include(m => m.Resolutions)
            .Include(m => m.Candidates)
            .FirstAsync(m => m.Id == meetingId);

        meeting.Title.Should().Be("ĐHĐCĐ 2025");
        meeting.Status.Should().Be(MeetingStatus.New);
        meeting.Resolutions.Should().HaveCount(2);
        meeting.Candidates.Should().HaveCount(2);
    }

    // TC-02: Create → then verify audit_log entry exists
    [Fact]
    public async Task CreateMeeting_AuditLogCreated()
    {
        var mediator = _fixture.CreateMediator();
        var cmd = await MakeCreateCommand();

        var meetingId = await mediator.Send(cmd);

        var db = _fixture.CreateFreshDbContext();
        var auditEntry = await db.AuditLogs
            .Where(a => a.EntityId == meetingId)
            .FirstOrDefaultAsync();

        auditEntry.Should().NotBeNull();
        auditEntry!.Detail.Should().Contain("Meeting created");
    }

    // TC-03: Delete meeting WITHOUT shareholders → soft-deletes (IsDeleted = true)
    [Fact]
    public async Task DeleteMeeting_WithoutShareholders_SoftDeletes()
    {
        var mediator = _fixture.CreateMediator();
        var cmd = await MakeCreateCommand();
        var meetingId = await mediator.Send(cmd);

        // Now delete
        await mediator.Send(new DeleteMeetingCommand(meetingId));

        var db = _fixture.CreateFreshDbContext();
        var meeting = await db.Meetings
            .IgnoreQueryFilters()
            .FirstAsync(m => m.Id == meetingId);

        meeting.IsDeleted.Should().BeTrue();
    }

    // TC-04: Delete meeting WITH shareholders → throws InvalidOperationException
    [Fact]
    public async Task DeleteMeeting_WithShareholders_ThrowsBusinessException()
    {
        var mediator = _fixture.CreateMediator();
        var cmd = await MakeCreateCommand();
        var meetingId = await mediator.Send(cmd);

        // Add a shareholder directly to DB
        var db = _fixture.CreateFreshDbContext();
        db.Shareholders.Add(new Shareholder
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            FullName = "Cổ đông Test",
            IdNumber = "CMT_TEST_001",
            VotingRights = 100,
            VsdcRow = "1.1",
            DisplayOrder = 1,
        });
        await db.SaveChangesAsync();

        // Try delete → should throw
        Func<Task> act = () => mediator.Send(new DeleteMeetingCommand(meetingId));
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cổ đông*");
    }

    // TC-05: Clone meeting → copies resolutions + candidates, NOT shareholders
    [Fact]
    public async Task CloneMeeting_CopiesResolutionsAndCandidates_NotShareholders()
    {
        var mediator = _fixture.CreateMediator();
        var cmd = await MakeCreateCommand();
        var sourceId = await mediator.Send(cmd);

        // Add a shareholder to source meeting
        var db1 = _fixture.CreateFreshDbContext();
        db1.Shareholders.Add(new Shareholder
        {
            Id = Guid.NewGuid(),
            MeetingId = sourceId,
            FullName = "Cổ đông nguồn",
            IdNumber = "CMT_SRC_001",
            VotingRights = 500,
            VsdcRow = "1.1",
            DisplayOrder = 1,
        });
        await db1.SaveChangesAsync();

        // Clone
        var cloneId = await mediator.Send(new CloneMeetingCommand(sourceId));

        var db2 = _fixture.CreateFreshDbContext();
        var clone = await db2.Meetings
            .Include(m => m.Resolutions)
            .Include(m => m.Candidates)
            .FirstAsync(m => m.Id == cloneId);

        clone.Title.Should().Contain("Bản sao");
        clone.Resolutions.Should().HaveCount(2);
        clone.Candidates.Should().HaveCount(2);

        // Shareholders should NOT be cloned
        var shareholderCount = await db2.Shareholders
            .CountAsync(s => s.MeetingId == cloneId);
        shareholderCount.Should().Be(0);
    }
}
