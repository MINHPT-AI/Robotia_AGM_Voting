using FluentAssertions;
using Mms.Application.Meetings.Commands;
using Mms.Application.Meetings.Dtos;
using Mms.Application.Meetings.Validators;
using Mms.Domain.Enums;

namespace Mms.UnitTests.Validators;

/// <summary>
/// Tests for CreateMeetingValidator.
/// VERIFIED rules from CreateMeetingValidator.cs:
///   1. Title: NotEmpty + MaximumLength(500)
///   2. MeetingDate: GreaterThan(UtcNow - 1h)
///   3. Location: NotEmpty + MaximumLength(500)
///   4. TotalVotingShares: GreaterThan(0)
///   5. CompanyId: NotEmpty
/// </summary>
public class CreateMeetingValidatorTests
{
    private readonly CreateMeetingValidator _validator = new();

    private static CreateMeetingCommand MakeValidCommand() => new(
        CompanyId: Guid.NewGuid(),
        Title: "ĐHĐCĐ thường niên 2025",
        MeetingType: MeetingType.Annual,
        MeetingDate: DateTime.UtcNow.AddDays(7),
        Location: "Khách sạn Rex, TP.HCM",
        RecordDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
        TotalVotingShares: 10_000_000,
        Chairman: "Nguyễn Văn A",
        Secretary: "Trần Thị B",
        Notes: null,
        Resolutions: new List<ResolutionDto>(),
        Candidates: new List<CandidateDto>());

    // TC-01: Valid command → no validation errors
    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(MakeValidCommand());

        result.IsValid.Should().BeTrue();
    }

    // TC-02: Empty Title → validation fails
    [Fact]
    public void Validate_EmptyTitle_Fails()
    {
        var cmd = MakeValidCommand() with { Title = "" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    // TC-03: MeetingDate in the past → validation fails
    [Fact]
    public void Validate_PastMeetingDate_Fails()
    {
        var cmd = MakeValidCommand() with { MeetingDate = DateTime.UtcNow.AddDays(-5) };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MeetingDate");
    }

    // TC-04: TotalVotingShares = 0 → validation fails
    [Fact]
    public void Validate_ZeroTotalVotingShares_Fails()
    {
        var cmd = MakeValidCommand() with { TotalVotingShares = 0 };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalVotingShares");
    }

    // TC-05: Empty CompanyId → validation fails
    [Fact]
    public void Validate_EmptyCompanyId_Fails()
    {
        var cmd = MakeValidCommand() with { CompanyId = Guid.Empty };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyId");
    }
}
