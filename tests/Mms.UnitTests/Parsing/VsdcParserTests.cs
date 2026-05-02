using ClosedXML.Excel;
using FluentAssertions;
using Mms.Infrastructure.Parsing;
using Mms.UnitTests.Helpers;

namespace Mms.UnitTests.Parsing;

public class VsdcParserTests
{
    private readonly VsdcParser _parser = new();

    // TC-01: Happy path — sample file with 3 rows → returns 3 data rows
    [Fact]
    public void Parse_MultipleValidRows_ReturnsCorrectRowCount()
    {
        var rows = new List<string?[]>
        {
            VsdcXlsxBuilder.MakeDataRow("Nguyễn A", "CMT001", "1.000"),
            VsdcXlsxBuilder.MakeDataRow("Trần B", "CMT002", "2.000"),
            VsdcXlsxBuilder.MakeDataRow("Lê C", "CMT003", "3.000"),
        };
        using var stream = VsdcXlsxBuilder.Build(b =>
            b.AddSection("I. MÔI GIỚI TRONG NƯỚC", "1. Cá nhân", rows));

        var result = _parser.Parse(stream);

        result.Rows.Should().HaveCount(3);
    }

    // TC-02: Column 5 (IdNumber) is read correctly via column map
    [Fact]
    public void Parse_Column5_IsIdNumber()
    {
        using var stream = VsdcXlsxBuilder.BuildSingleRow(
            idNumber: "024680135");

        var result = _parser.Parse(stream);

        result.Rows.Should().HaveCount(1);
        // Cells[4] = column 5 (IdNumber) — 0-indexed from 16-cell array
        result.Rows[0].Cells[4]?.Trim().Should().Be("024680135");
    }

    // TC-03: Column 10 (Nationality) is read correctly
    [Fact]
    public void Parse_Column10_IsNationality()
    {
        using var stream = VsdcXlsxBuilder.BuildSingleRow(
            nationality: "Nhật Bản");

        var result = _parser.Parse(stream);

        result.Rows.Should().HaveCount(1);
        // Cells[9] = column 10 (Nationality)
        result.Rows[0].Cells[9]?.Trim().Should().Be("Nhật Bản");
    }

    // TC-04: Column 16 (VotingRights) is positive text that can be parsed
    [Fact]
    public void Parse_Column16_VotingRightsIsReadableText()
    {
        using var stream = VsdcXlsxBuilder.BuildSingleRow(
            votingRights: "5.000");

        var result = _parser.Parse(stream);

        result.Rows.Should().HaveCount(1);
        // Cells[15] = column 16 (VotingRights)
        var votingText = result.Rows[0].Cells[15]?.Trim();
        votingText.Should().NotBeNullOrWhiteSpace();
        VsdcRowMapper.ParseVsdcNumber(votingText).Should().Be(5000);
    }

    // TC-05: File without "STT" header → throws VsdcFormatException
    [Fact]
    public void Parse_FileWithoutHeader_ThrowsVsdcFormatException()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "Random content";
        ws.Cell(2, 1).Value = "No STT header anywhere";
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var act = () => _parser.Parse(stream);

        act.Should().Throw<VsdcFormatException>()
            .WithMessage("*STT*");
    }

    // TC-06: File with header but incomplete number row (<16 cols) → throws
    [Fact]
    public void Parse_IncompleteNumberRow_ThrowsVsdcFormatException()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("VSDC");
        // Add "STT" header at row 5
        ws.Cell(5, 2).Value = "STT";
        // Number row at row 7 — only 5 columns instead of 16
        ws.Cell(7, 2).Value = 1;
        ws.Cell(7, 3).Value = 2;
        ws.Cell(7, 4).Value = 3;
        ws.Cell(7, 6).Value = 4;
        ws.Cell(7, 8).Value = 5;
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var act = () => _parser.Parse(stream);

        act.Should().Throw<VsdcFormatException>()
            .WithMessage("*5/16*");
    }

    // TC-07: Vietnamese thousands format "18.600" → 18600 via RowMapper
    [Fact]
    public void Parse_VietnameseThousandsFormat_ParsesCorrectly()
    {
        using var stream = VsdcXlsxBuilder.BuildSingleRow(
            votingRights: "18.600");

        var result = _parser.Parse(stream);
        var mapped = VsdcRowMapper.Map(result.Rows[0], 1);

        mapped.VotingRights.Should().Be(18600);
    }

    // TC-08: Section II marker → SectionType = "II" → IsForeign = true
    [Fact]
    public void Parse_SectionIIMarker_TagsAsForeign()
    {
        using var stream = VsdcXlsxBuilder.Build(b =>
        {
            b.AddSection("I. MÔI GIỚI TRONG NƯỚC", "1. Cá nhân",
                new List<string?[]> { VsdcXlsxBuilder.MakeDataRow("Domestic", "D001") });
            b.AddSection("II. MÔI GIỚI NƯỚC NGOÀI", "1. Cá nhân",
                new List<string?[]> { VsdcXlsxBuilder.MakeDataRow("Foreign", "F001") });
        });

        var result = _parser.Parse(stream);

        result.Rows.Should().HaveCount(2);

        // First row — Section I (domestic)
        result.Rows[0].SectionType.Should().Be("I");
        VsdcRowMapper.Map(result.Rows[0], 1).IsForeign.Should().BeFalse();

        // Second row — Section II (foreign)
        result.Rows[1].SectionType.Should().Be("II");
        VsdcRowMapper.Map(result.Rows[1], 2).IsForeign.Should().BeTrue();
    }
}
