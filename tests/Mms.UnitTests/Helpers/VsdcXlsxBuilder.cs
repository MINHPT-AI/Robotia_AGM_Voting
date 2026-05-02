using ClosedXML.Excel;

namespace Mms.UnitTests.Helpers;

/// <summary>
/// Helper to build VSDC-format xlsx files in-memory for testing VsdcParser.
/// Matches the actual VSDC structure: metadata rows, header "STT", number row "1"..&quot;16&quot;, 
/// section/sub-section markers, data rows, subtotals, grand total.
/// </summary>
public static class VsdcXlsxBuilder
{
    public static MemoryStream Build(Action<VsdcBuilder> configure)
    {
        var b = new VsdcBuilder();
        configure(b);
        return b.ToStream();
    }

    public class VsdcBuilder
    {
        public List<(string Section, string SubSection, List<string?[]> Rows)> Sections = new();

        public VsdcBuilder AddSection(string section, string subSection, List<string?[]> rows)
        {
            Sections.Add((section, subSection, rows));
            return this;
        }

        public MemoryStream ToStream()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("VSDC");

            // Rows 1-12: metadata (dummy — parser scans first 50 rows for "STT")
            ws.Cell(1, 1).Value = "TRUNG TÂM LƯU KÝ CHỨNG KHOÁN VIỆT NAM";
            ws.Cell(7, 1).Value = "DANH SÁCH TỔNG HỢP NGƯỜI SỞ HỮU";

            // Row 13: Header row — parser looks for "STT" in columns 1-5
            ws.Cell(13, 2).Value = "STT";
            ws.Cell(13, 3).Value = "Họ và tên";
            ws.Cell(13, 8).Value = "Số ĐKSH";

            // Row 14: Sub-header (skipped by parser)
            ws.Cell(14, 2).Value = "";

            // Row 15: Number row — CRITICAL for BuildColumnMap
            // Parser reads cells looking for integers 1..16 to build physical column mapping
            ws.Cell(15, 2).Value = 1;   // STT
            ws.Cell(15, 3).Value = 2;   // FullName
            ws.Cell(15, 4).Value = 3;   // SID
            ws.Cell(15, 6).Value = 4;   // InvestorCode
            ws.Cell(15, 8).Value = 5;   // IdNumber
            ws.Cell(15, 10).Value = 6;  // IdIssueDate
            ws.Cell(15, 12).Value = 7;  // Address
            ws.Cell(15, 14).Value = 8;  // Email
            ws.Cell(15, 15).Value = 9;  // Phone
            ws.Cell(15, 17).Value = 10; // Nationality
            ws.Cell(15, 18).Value = 11; // SharesNonDeposit
            ws.Cell(15, 20).Value = 12; // SharesDeposit
            ws.Cell(15, 21).Value = 13; // SharesTotal
            ws.Cell(15, 23).Value = 14; // RightsNonDeposit
            ws.Cell(15, 26).Value = 15; // RightsDeposit
            ws.Cell(15, 27).Value = 16; // VotingRights

            int currentRow = 16;
            foreach (var sec in Sections)
            {
                // Section header (e.g., "I. MÔI GIỚI TRONG NƯỚC")
                ws.Cell(currentRow++, 2).Value = sec.Section;
                // Sub-section header (e.g., "1. Cá nhân")
                ws.Cell(currentRow++, 2).Value = sec.SubSection;

                // Physical column indices matching the number row mapping
                var physicalCols = new[] { 2, 3, 4, 6, 8, 10, 12, 14, 15, 17, 18, 20, 21, 23, 26, 27 };

                foreach (var row in sec.Rows)
                {
                    for (int i = 0; i < row.Length && i < physicalCols.Length; i++)
                    {
                        if (row[i] != null)
                            ws.Cell(currentRow, physicalCols[i]).Value = row[i];
                    }
                    currentRow++;
                }

                // Subtotal row
                ws.Cell(currentRow++, 2).Value = "Cộng";
            }

            // Grand total
            ws.Cell(currentRow, 2).Value = "TỔNG CỘNG";

            var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
    }

    /// <summary>
    /// Creates a valid VSDC file with a single row for basic parsing tests.
    /// </summary>
    public static MemoryStream BuildSingleRow(
        string fullName = "Nguyễn Văn A",
        string idNumber = "012345678",
        string votingRights = "1.000",
        string nationality = "Việt Nam",
        string section = "I. MÔI GIỚI TRONG NƯỚC",
        string subSection = "1. Cá nhân")
    {
        return Build(b => b.AddSection(section, subSection,
            new List<string?[]> { MakeDataRow(fullName, idNumber, votingRights, nationality) }));
    }

    /// <summary>
    /// Creates a valid VSDC file with N rows for performance/bulk tests.
    /// </summary>
    public static MemoryStream BuildRows(int count)
    {
        var rows = new List<string?[]>();
        for (int i = 1; i <= count; i++)
        {
            rows.Add(new string?[]
            {
                $"1.{i}",                // STT
                $"Nguyễn Văn {i}",       // FullName
                $"SID{i:D8}",            // SID
                $"INV{i:D6}",            // InvestorCode
                $"CMT{i:D9}",            // IdNumber
                "01/01/2000",            // IdIssueDate
                $"Số {i}, Đường ABC",    // Address
                $"email{i}@test.vn",     // Email
                $"09{i:D8}",             // Phone
                "Việt Nam",              // Nationality
                "0", "1.000", "1.000",   // Shares
                "0", "1.000", "1.000"    // Rights (VotingRights = 1000)
            });
        }
        return Build(b => b.AddSection("I. MÔI GIỚI TRONG NƯỚC", "1. Cá nhân", rows));
    }

    public static string?[] MakeDataRow(
        string fullName = "Nguyễn Văn A",
        string idNumber = "012345678",
        string votingRights = "1.000",
        string nationality = "Việt Nam")
    {
        return new string?[]
        {
            "1.1", fullName, "SID00001", "INV001", idNumber,
            "01/01/2020", "123 Đường ABC", "test@test.vn", "0123456789",
            nationality, "0", "1.000", "1.000", "0", votingRights, votingRights
        };
    }
}
