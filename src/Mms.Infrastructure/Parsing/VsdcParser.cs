using ClosedXML.Excel;

namespace Mms.Infrastructure.Parsing;

public class VsdcParser
{
    private const int HeaderScanMax = 50;

    public VsdcParseResult Parse(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var ws = workbook.Worksheet(1);

        // GIAI ĐOẠN 1 — Locate header + build column map
        int headerRow = FindHeaderRow(ws);
        int numberRow = headerRow + 2;
        int[] columnMap = BuildColumnMap(ws, numberRow);

        // GIAI ĐOẠN 2 — Extract data rows
        int dataStartRow = numberRow + 1;
        var rows = ExtractDataRows(ws, dataStartRow, columnMap);

        return new VsdcParseResult(rows, headerRow, dataStartRow, columnMap);
    }

    private int FindHeaderRow(IXLWorksheet ws)
    {
        var lastRow = Math.Min(HeaderScanMax, ws.LastRowUsed()?.RowNumber() ?? 0);
        for (int r = 1; r <= lastRow; r++)
        {
            // Header "STT" có thể ở các cột đầu tiên (B thường gặp nhất)
            for (int c = 1; c <= 5; c++)
            {
                var v = ws.Cell(r, c).GetString()?.Trim();
                if (string.Equals(v, "STT", StringComparison.OrdinalIgnoreCase))
                    return r;
            }
        }

        throw new VsdcFormatException(
            "Không tìm thấy dòng tiêu đề 'STT' trong 50 dòng đầu. File không đúng định dạng VSDC.");
    }

    private int[] BuildColumnMap(IXLWorksheet ws, int numberRow)
    {
        var map = new int[17]; // index 0 unused, 1..16 = physical column
        var foundCount = 0;
        var lastCol = ws.Row(numberRow).LastCellUsed()?.Address.ColumnNumber ?? 30;

        for (int c = 1; c <= lastCol; c++)
        {
            var txt = ws.Cell(numberRow, c).GetString()?.Trim();
            if (int.TryParse(txt, out var idx) && idx >= 1 && idx <= 16 && map[idx] == 0)
            {
                map[idx] = c;
                foundCount++;
            }
        }

        if (foundCount < 16)
            throw new VsdcFormatException(
                $"Dòng số cột chỉ có {foundCount}/16 cột. File VSDC không đầy đủ định dạng.",
                rowIndex: numberRow);

        return map;
    }

    private List<VsdcRawRow> ExtractDataRows(IXLWorksheet ws, int startRow, int[] columnMap)
    {
        var rows = new List<VsdcRawRow>();
        var currentSection = "";
        var currentSubSection = "";
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int r = startRow; r <= lastRow; r++)
        {
            // ⚠️ CRITICAL: check nhiều cột vì merged cells có thể khiến STT col rỗng
            var cellSTT = ws.Cell(r, columnMap[1]).GetString()?.Trim() ?? "";
            var cellName = ws.Cell(r, columnMap[2]).GetString()?.Trim() ?? "";
            var cellSid = ws.Cell(r, columnMap[3]).GetString()?.Trim() ?? "";

            // Union text để phát hiện section/grand total dù cell bị merge
            var anyText = $"{cellSTT}|{cellName}|{cellSid}";

            // --- Phát hiện kết thúc ---
            if (anyText.Contains("TỔNG CỘNG", StringComparison.OrdinalIgnoreCase)
                && !anyText.Contains("CỘNG:", StringComparison.OrdinalIgnoreCase))
                break;

            // --- Section header: "I." hoặc "II." ---
            var sectionMatch = DetectSection(cellSTT, cellName, cellSid);
            if (sectionMatch != null)
            {
                currentSection = sectionMatch;
                currentSubSection = "";  // ⚠️ RESET khi sang section mới
                continue;
            }

            // --- Sub-section: "1. Cá nhân" / "2. Tổ chức" ---
            var subMatch = DetectSubSection(cellSTT, cellName, cellSid);
            if (subMatch != null)
            {
                currentSubSection = subMatch;
                continue;
            }

            // --- Subtotal "Cộng" hoặc "Cộng: ..." ---
            if (cellSTT.StartsWith("Cộng", StringComparison.OrdinalIgnoreCase) ||
                cellName.StartsWith("Cộng", StringComparison.OrdinalIgnoreCase))
                continue;

            // --- Dòng trống ---
            if (string.IsNullOrWhiteSpace(cellSTT) && string.IsNullOrWhiteSpace(cellName))
                continue;

            // --- Data row: phải có FullName + IdNumber ---
            var idNumberCell = ws.Cell(r, columnMap[5]).GetString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(cellName) || string.IsNullOrWhiteSpace(idNumberCell))
                continue;

            // Đọc đủ 16 cells theo mapping
            var cells = new string?[16];
            for (int i = 0; i < 16; i++)
                cells[i] = ws.Cell(r, columnMap[i + 1]).GetString();

            rows.Add(new VsdcRawRow(r, cells, currentSection, currentSubSection));
        }

        return rows;
    }

    private static string? DetectSection(params string[] candidates)
    {
        foreach (var text in candidates)
        {
            var t = text.Trim();
            if (t.StartsWith("I.", StringComparison.Ordinal) &&
                t.Contains("MÔI GIỚI", StringComparison.OrdinalIgnoreCase) &&
                t.Contains("TRONG NƯỚC", StringComparison.OrdinalIgnoreCase))
                return "I";
            if (t.StartsWith("II.", StringComparison.Ordinal) &&
                t.Contains("MÔI GIỚI", StringComparison.OrdinalIgnoreCase) &&
                t.Contains("NƯỚC NGOÀI", StringComparison.OrdinalIgnoreCase))
                return "II";
        }
        return null;
    }

    private static string? DetectSubSection(params string[] candidates)
    {
        foreach (var text in candidates)
        {
            var t = text.Trim();
            if (t.StartsWith("1.", StringComparison.Ordinal) &&
                t.Contains("Cá nhân", StringComparison.OrdinalIgnoreCase))
                return "1. Cá nhân";
            if (t.StartsWith("2.", StringComparison.Ordinal) &&
                t.Contains("Tổ chức", StringComparison.OrdinalIgnoreCase))
                return "2. Tổ chức";
        }
        return null;
    }
}
