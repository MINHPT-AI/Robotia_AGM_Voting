using FluentAssertions;
using Mms.Infrastructure.Parsing;

namespace Mms.UnitTests.Parsing;

public class VsdcRowMapperTests
{
    // TC-01: Valid raw row → maps all fields correctly
    [Fact]
    public void Map_ValidRow_ReturnsCorrectDto()
    {
        var raw = new VsdcRawRow(
            RowIndex: 20,
            Cells: new string?[]
            {
                "1.1",               // [0] STT (VsdcRow)
                "Nguyễn Văn A",      // [1] FullName
                "SID001",            // [2] Sid
                "INV001",            // [3] InvestorCode
                "012345678",         // [4] IdNumber
                "15/06/2020",        // [5] IdIssueDate
                "123 Đường ABC",     // [6] Address
                "a@b.vn",            // [7] Email
                "0909123456",        // [8] Phone
                "Việt Nam",          // [9] Nationality
                "0",                 // [10] SharesNonDeposit
                "5.000",             // [11] SharesDeposit
                "5.000",             // [12] SharesTotal
                "0",                 // [13] RightsNonDeposit
                "5.000",             // [14] RightsDeposit
                "5.000",             // [15] VotingRights
            },
            SectionType: "I",
            SubSectionType: "1. Cá nhân");

        var dto = VsdcRowMapper.Map(raw, displayOrder: 1);

        dto.RowIndex.Should().Be(20);
        dto.DisplayOrder.Should().Be(1);
        dto.VsdcRow.Should().Be("1.1");
        dto.FullName.Should().Be("Nguyễn Văn A");
        dto.IdNumber.Should().Be("012345678");
        dto.Nationality.Should().Be("Việt Nam");
        dto.VotingRights.Should().Be(5000);
        dto.SharesTotal.Should().Be(5000);
        dto.IsOrganization.Should().BeFalse();
        dto.IsForeign.Should().BeFalse();
    }

    // TC-02: Null cells → returns empty strings / null / 0 (no exception)
    [Fact]
    public void Map_NullCells_HandlesGracefully()
    {
        var raw = new VsdcRawRow(
            RowIndex: 1,
            Cells: new string?[] { null, null, null, null, null, null, null, null,
                                   null, null, null, null, null, null, null, null },
            SectionType: "I",
            SubSectionType: "1. Cá nhân");

        var dto = VsdcRowMapper.Map(raw, 1);

        dto.FullName.Should().BeEmpty();
        dto.IdNumber.Should().BeEmpty();
        dto.Sid.Should().BeNull();
        dto.Email.Should().BeNull();
        dto.VotingRights.Should().Be(0);
        dto.IdIssueDate.Should().BeNull();
    }

    // TC-03: dd/MM/yyyy date format → parses correctly
    [Fact]
    public void Map_DdMmYyyyDate_ParsesCorrectly()
    {
        var result = VsdcRowMapper.ParseVsdcDate("25/12/2023");

        result.Should().Be(new DateOnly(2023, 12, 25));
    }

    // TC-04: Excel OADate number → parses correctly
    [Fact]
    public void Map_OADateNumber_ParsesCorrectly()
    {
        // OADate for 2023-06-15 = 45092
        var oaDate = new DateTime(2023, 6, 15).ToOADate().ToString();
        var result = VsdcRowMapper.ParseVsdcDate(oaDate);

        result.Should().Be(new DateOnly(2023, 6, 15));
    }

    // Bonus: ParseVsdcNumber edge cases
    [Theory]
    [InlineData("18.600", 18600)]       // Vietnamese thousands separator
    [InlineData("7.952.200", 7952200)]  // Multiple separators
    [InlineData("1000", 1000)]          // No separator
    [InlineData("", 0)]                 // Empty
    [InlineData(null, 0)]               // Null
    [InlineData("abc", 0)]              // Non-numeric
    public void ParseVsdcNumber_VariousFormats_ParsesCorrectly(string? input, long expected)
    {
        VsdcRowMapper.ParseVsdcNumber(input).Should().Be(expected);
    }
}
