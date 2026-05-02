using Mms.Application.Shareholders.Dtos;

namespace Mms.Infrastructure.Parsing;

public class VsdcValidator
{
    public VsdcValidationResult Validate(
        List<ShareholderImportDto> rows,
        HashSet<string> existingIdNumbersInDb,
        long totalCharterVotingShares)
    {
        var errors = new List<VsdcRowError>();
        var warnings = new List<VsdcRowWarning>();
        var seenIdNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rows)
        {
            // Rule 1: MISSING_ID_NUMBER (warning — VSDC may have edge cases)
            if (string.IsNullOrWhiteSpace(r.IdNumber))
            {
                warnings.Add(new(r.RowIndex, "MISSING_ID_NUMBER", "Thiếu Số ĐKSH (cột 5)"));
                continue;
            }

            // Rule 2: MISSING_NAME (warning)
            if (string.IsNullOrWhiteSpace(r.FullName))
                warnings.Add(new(r.RowIndex, "MISSING_NAME", "Thiếu Họ và tên (cột 2)"));

            // Rule 3: ZERO_VOTING_RIGHTS (warning)
            if (r.VotingRights <= 0)
                warnings.Add(new(r.RowIndex, "ZERO_VOTING_RIGHTS",
                    $"Quyền biểu quyết = {r.VotingRights}, phải > 0"));

            // Rule 4: INTRA_FILE_DUPLICATE (warning — VSDC dùng ID + ngày cấp để phân biệt,
            // cùng ID nhưng khác ngày cấp vẫn là 2 NĐT khác nhau)
            if (seenIdNumbers.TryGetValue(r.IdNumber, out var prevRow))
                warnings.Add(new(r.RowIndex, "INTRA_FILE_DUPLICATE",
                    $"Số ĐKSH '{r.IdNumber}' đã xuất hiện ở dòng {prevRow}"));
            else
                seenIdNumbers[r.IdNumber] = r.RowIndex;

            // Rule 5: DUPLICATE_IN_DB (warning — sẽ cập nhật, không lỗi)
            if (existingIdNumbersInDb.Contains(r.IdNumber))
                warnings.Add(new(r.RowIndex, "DUPLICATE_IN_DB",
                    $"Cổ đông '{r.FullName}' (ĐKSH: {r.IdNumber}) đã tồn tại → sẽ được cập nhật"));
        }

        // Rule 6: EXCEEDS_CHARTER (warning)
        var totalVotingInFile = rows.Where(r => r.VotingRights > 0).Sum(r => r.VotingRights);
        if (totalCharterVotingShares > 0 && totalVotingInFile > totalCharterVotingShares)
            warnings.Add(new(0, "EXCEEDS_CHARTER",
                $"Tổng quyền BQ trong file ({totalVotingInFile:N0}) > " +
                $"Vốn điều lệ đã đăng ký ({totalCharterVotingShares:N0})"));

        return new VsdcValidationResult(errors, warnings);
    }
}
