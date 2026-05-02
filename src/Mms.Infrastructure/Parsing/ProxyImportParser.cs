using ClosedXML.Excel;

namespace Mms.Infrastructure.Parsing;

/// <summary>
/// Dòng dữ liệu thô từ file Excel import ủy quyền.
/// </summary>
public record ProxyImportRawRow(
    int RowNumber,
    int Stt,
    string GrantorName,
    string GrantorIdNumber,
    string GranteeName,
    string GranteeIdNumber,
    long Shares,
    string? GranteePhone);

/// <summary>
/// Kết quả parse file Excel import ủy quyền.
/// </summary>
public record ProxyImportParseResult(
    List<ProxyImportRawRow> Rows,
    List<string> Errors);

public class ProxyImportParser
{
    /// <summary>
    /// Parse file Excel ủy quyền với format:
    /// STT | Cổ đông ủy quyền | Số ĐKSH | Người nhận ủy quyền | Số ĐKSH | Số cổ phần UQ | SĐT Người nhận (optional)
    /// </summary>
    public ProxyImportParseResult Parse(Stream xlsxStream)
    {
        var rows = new List<ProxyImportRawRow>();
        var errors = new List<string>();

        using var workbook = new XLWorkbook(xlsxStream);
        var ws = workbook.Worksheet(1);

        // Find header row (look for "STT" in first 20 rows)
        int headerRow = 0;
        for (int r = 1; r <= Math.Min(20, ws.LastRowUsed()?.RowNumber() ?? 0); r++)
        {
            for (int c = 1; c <= 5; c++)
            {
                var v = ws.Cell(r, c).GetString()?.Trim();
                if (string.Equals(v, "STT", StringComparison.OrdinalIgnoreCase))
                {
                    headerRow = r;
                    break;
                }
            }
            if (headerRow > 0) break;
        }

        if (headerRow == 0)
        {
            errors.Add("Không tìm thấy dòng tiêu đề 'STT'. File không đúng định dạng.");
            return new ProxyImportParseResult(rows, errors);
        }

        int dataStartRow = headerRow + 1;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int r = dataStartRow; r <= lastRow; r++)
        {
            // Determine column offset (STT might be col 1 or col 2)
            int colOffset = 0;
            for (int c = 1; c <= 3; c++)
            {
                var hdr = ws.Cell(headerRow, c).GetString()?.Trim();
                if (string.Equals(hdr, "STT", StringComparison.OrdinalIgnoreCase))
                {
                    colOffset = c - 1; // 0-based offset
                    break;
                }
            }

            var sttText = ws.Cell(r, 1 + colOffset).GetString()?.Trim() ?? "";

            // Skip empty rows
            if (string.IsNullOrWhiteSpace(sttText))
                continue;

            // Try parse STT as number
            if (!int.TryParse(sttText, out var stt))
                continue; // Skip non-data rows (subtotals, etc.)

            var grantorName = ws.Cell(r, 2 + colOffset).GetString()?.Trim() ?? "";
            var grantorId = ws.Cell(r, 3 + colOffset).GetString()?.Trim() ?? "";
            var granteeName = ws.Cell(r, 4 + colOffset).GetString()?.Trim() ?? "";
            var granteeId = ws.Cell(r, 5 + colOffset).GetString()?.Trim() ?? "";
            var sharesText = ws.Cell(r, 6 + colOffset).GetString()?.Trim() ?? "0";
            var phone = ws.Cell(r, 7 + colOffset).GetString()?.Trim();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(grantorName) || string.IsNullOrWhiteSpace(grantorId))
            {
                errors.Add($"Dòng {r}: Thiếu thông tin Cổ đông ủy quyền hoặc Số ĐKSH.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(granteeName) || string.IsNullOrWhiteSpace(granteeId))
            {
                errors.Add($"Dòng {r}: Thiếu thông tin Người nhận ủy quyền hoặc Số ĐKSH.");
                continue;
            }

            // Parse shares (remove commas, dots for thousands separator)
            var cleanShares = sharesText.Replace(",", "").Replace(".", "").Trim();
            if (!long.TryParse(cleanShares, out var shares) || shares <= 0)
            {
                errors.Add($"Dòng {r}: Số cổ phần '{sharesText}' không hợp lệ.");
                continue;
            }

            rows.Add(new ProxyImportRawRow(r, stt, grantorName, grantorId, granteeName, granteeId, shares,
                string.IsNullOrWhiteSpace(phone) ? null : phone));
        }

        if (rows.Count == 0 && errors.Count == 0)
        {
            errors.Add("Không tìm thấy dòng dữ liệu hợp lệ nào trong file.");
        }

        return new ProxyImportParseResult(rows, errors);
    }
}
