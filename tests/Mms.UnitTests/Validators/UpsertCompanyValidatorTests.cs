using FluentAssertions;
using Mms.Application.Companies.Commands;
using Mms.Application.Companies.Validators;

namespace Mms.UnitTests.Validators;

/// <summary>
/// Tests for UpsertCompanyValidator.
/// VERIFIED rules from UpsertCompanyValidator.cs:
///   1. Name: NotEmpty + MaximumLength(255)
///   2. TaxCode: NotEmpty + Matches("^[0-9]{10}([0-9]{3})?$")  → 10 or 13 digits
///   3. LegalRepName: NotEmpty + MaximumLength(255)
///   4. LegalRepTitle: NotEmpty + MaximumLength(100)
///   5. CharterCapital: GreaterThan(0)
///   6. TotalSharesIssued: GreaterThan(0)
///   7. TotalVotingShares: GreaterThan(0) + LessThanOrEqualTo(TotalSharesIssued)
/// </summary>
public class UpsertCompanyValidatorTests
{
    private readonly UpsertCompanyValidator _validator = new();

    private static UpsertCompanyCommand MakeValidCommand() => new(
        Id: null,
        Name: "Công ty CP ABC",
        ShortName: "ABC",
        EnglishName: "ABC Corp",
        TaxCode: "0123456789",  // 10 digits
        StockCode: "ABC",
        StockExchange: "HOSE",
        Address: "123 Nguyễn Huệ",
        Phone: "028-12345678",
        Email: "info@abc.vn",
        Fax: null,
        Website: "https://abc.vn",
        LegalRepName: "Nguyễn Văn A",
        LegalRepTitle: "Giám đốc",
        CharterCapital: 100_000_000_000,
        TotalSharesIssued: 10_000_000,
        TotalVotingShares: 9_000_000,
        LogoPath: null,
        SealImagePath: null,
        SignatureImagePath: null);

    // TC-01: Valid command → no errors
    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(MakeValidCommand());

        result.IsValid.Should().BeTrue();
    }

    // TC-02: TaxCode with 10 digits → valid; TaxCode with 13 digits → valid
    [Theory]
    [InlineData("0123456789")]      // 10 digits
    [InlineData("0123456789012")]    // 13 digits
    public void Validate_ValidTaxCode_Passes(string taxCode)
    {
        var cmd = MakeValidCommand() with { TaxCode = taxCode };

        var result = _validator.Validate(cmd);

        result.Errors.Should().NotContain(e => e.PropertyName == "TaxCode");
    }

    // TC-03: Invalid TaxCode formats → fails
    [Theory]
    [InlineData("")]                // Empty
    [InlineData("12345")]           // Too short
    [InlineData("12345678901")]     // 11 digits (neither 10 nor 13)
    [InlineData("ABC1234567")]      // Contains letters
    public void Validate_InvalidTaxCode_Fails(string taxCode)
    {
        var cmd = MakeValidCommand() with { TaxCode = taxCode };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxCode");
    }

    // TC-04: TotalVotingShares > TotalSharesIssued → fails
    [Fact]
    public void Validate_VotingSharesExceedsIssued_Fails()
    {
        var cmd = MakeValidCommand() with
        {
            TotalSharesIssued = 1_000_000,
            TotalVotingShares = 2_000_000  // Exceeds
        };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalVotingShares");
    }

    // TC-05: CharterCapital = 0 → fails
    [Fact]
    public void Validate_ZeroCharterCapital_Fails()
    {
        var cmd = MakeValidCommand() with { CharterCapital = 0 };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CharterCapital");
    }

    // TC-06: Empty LegalRepName → fails
    [Fact]
    public void Validate_EmptyLegalRepName_Fails()
    {
        var cmd = MakeValidCommand() with { LegalRepName = "" };

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LegalRepName");
    }
}
