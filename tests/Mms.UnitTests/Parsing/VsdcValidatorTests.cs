using FluentAssertions;
using Mms.Application.Shareholders.Dtos;
using Mms.Infrastructure.Parsing;

namespace Mms.UnitTests.Parsing;

/// <summary>
/// Tests for VsdcValidator.
/// VERIFIED: All 6 rules in VsdcValidator.cs use Warnings.Add() — never Errors.Add().
/// The validator returns 0 errors in all cases (by design, VSDC data allows edge cases).
/// </summary>
public class VsdcValidatorTests
{
    private readonly VsdcValidator _validator = new();

    // TC-01: Row with empty IdNumber → MISSING_ID_NUMBER warning + skip remaining rules
    [Fact]
    public void Validate_MissingIdNumber_AddsWarning()
    {
        var rows = new List<ShareholderImportDto>
        {
            new() { RowIndex = 5, IdNumber = "", FullName = "Nguyễn A", VotingRights = 100 }
        };

        var result = _validator.Validate(rows, new HashSet<string>(), 0);

        result.Errors.Should().BeEmpty();
        result.Warnings.Should().ContainSingle(w => w.Code == "MISSING_ID_NUMBER");
        result.HasErrors.Should().BeFalse();
    }

    // TC-02: Row with empty FullName → MISSING_NAME warning
    [Fact]
    public void Validate_MissingName_AddsWarning()
    {
        var rows = new List<ShareholderImportDto>
        {
            new() { RowIndex = 3, IdNumber = "CMT001", FullName = "", VotingRights = 100 }
        };

        var result = _validator.Validate(rows, new HashSet<string>(), 0);

        result.Warnings.Should().Contain(w => w.Code == "MISSING_NAME");
    }

    // TC-03: Row with VotingRights = 0 → ZERO_VOTING_RIGHTS warning
    [Fact]
    public void Validate_ZeroVotingRights_AddsWarning()
    {
        var rows = new List<ShareholderImportDto>
        {
            new() { RowIndex = 4, IdNumber = "CMT002", FullName = "B", VotingRights = 0 }
        };

        var result = _validator.Validate(rows, new HashSet<string>(), 0);

        result.Warnings.Should().Contain(w => w.Code == "ZERO_VOTING_RIGHTS");
    }

    // TC-04: Two rows with same IdNumber → INTRA_FILE_DUPLICATE warning on second row
    [Fact]
    public void Validate_DuplicateIdNumberInFile_AddsWarning()
    {
        var rows = new List<ShareholderImportDto>
        {
            new() { RowIndex = 1, IdNumber = "CMT999", FullName = "A", VotingRights = 100 },
            new() { RowIndex = 2, IdNumber = "CMT999", FullName = "B", VotingRights = 200 },
        };

        var result = _validator.Validate(rows, new HashSet<string>(), 0);

        result.Warnings.Should().Contain(w =>
            w.Code == "INTRA_FILE_DUPLICATE" && w.RowIndex == 2);
    }

    // TC-05: Total voting rights in file exceeds charter capital → EXCEEDS_CHARTER warning
    [Fact]
    public void Validate_TotalExceedsCharterCapital_AddsWarning()
    {
        var rows = new List<ShareholderImportDto>
        {
            new() { RowIndex = 1, IdNumber = "CMT001", FullName = "A", VotingRights = 600 },
            new() { RowIndex = 2, IdNumber = "CMT002", FullName = "B", VotingRights = 500 },
        };

        // Total = 1100, charter = 1000 → exceeds
        var result = _validator.Validate(rows, new HashSet<string>(), totalCharterVotingShares: 1000);

        result.Warnings.Should().Contain(w =>
            w.Code == "EXCEEDS_CHARTER" && w.RowIndex == 0);
    }

    // TC-06: All valid rows → 0 errors, 0 warnings
    [Fact]
    public void Validate_AllValid_ReturnsNoErrorsOrWarnings()
    {
        var rows = new List<ShareholderImportDto>
        {
            new() { RowIndex = 1, IdNumber = "CMT001", FullName = "Nguyễn A", VotingRights = 500 },
            new() { RowIndex = 2, IdNumber = "CMT002", FullName = "Trần B", VotingRights = 300 },
        };

        // No existing IDs in DB, charter = 1000 (total=800, not exceeded)
        var result = _validator.Validate(rows, new HashSet<string>(), totalCharterVotingShares: 1000);

        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }
}
