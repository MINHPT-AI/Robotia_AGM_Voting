using Mms.Application.Shareholders.Dtos;
using System.Globalization;

namespace Mms.Infrastructure.Parsing;

public static class VsdcRowMapper
{
    public static ShareholderImportDto Map(VsdcRawRow raw, int displayOrder)
    {
        return new ShareholderImportDto
        {
            RowIndex = raw.RowIndex,
            DisplayOrder = displayOrder,
            VsdcRow = raw.Cells[0]?.Trim() ?? "",
            FullName = raw.Cells[1]?.Trim() ?? "",
            Sid = EmptyToNull(raw.Cells[2]),
            InvestorCode = EmptyToNull(raw.Cells[3]),
            IdNumber = raw.Cells[4]?.Trim() ?? "",
            IdIssueDate = ParseVsdcDate(raw.Cells[5]),
            Address = EmptyToNull(raw.Cells[6]),
            Email = EmptyToNull(raw.Cells[7]),
            Phone = EmptyToNull(raw.Cells[8]),
            Nationality = EmptyToNull(raw.Cells[9]),
            SharesNonDeposit = ParseVsdcNumber(raw.Cells[10]),
            SharesDeposit = ParseVsdcNumber(raw.Cells[11]),
            SharesTotal = ParseVsdcNumber(raw.Cells[12]),
            RightsNonDeposit = ParseVsdcNumber(raw.Cells[13]),
            RightsDeposit = ParseVsdcNumber(raw.Cells[14]),
            VotingRights = ParseVsdcNumber(raw.Cells[15]),
            IsOrganization = raw.SubSectionType.Contains("Tổ chức", StringComparison.OrdinalIgnoreCase),
            IsForeign = raw.SectionType == "II"
        };
    }

    /// <summary>
    /// VSDC số dùng dấu chấm (.) = hàng nghìn: "18.600" = 18600, "7.952.200" = 7952200
    /// </summary>
    public static long ParseVsdcNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var cleaned = value.Replace(".", "").Replace(" ", "").Replace(",", "").Trim();
        return long.TryParse(cleaned, out var result) ? result : 0;
    }

    /// <summary>
    /// VSDC ngày format dd/MM/yyyy. Cũng xử lý trường hợp Excel trả số OADate.
    /// </summary>
    public static DateOnly? ParseVsdcDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy" };
        if (DateOnly.TryParseExact(value.Trim(), formats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        // Excel có thể trả về dạng OADate number
        if (double.TryParse(value.Trim(), NumberStyles.Any,
            CultureInfo.InvariantCulture, out var oaDate) && oaDate > 1)
            return DateOnly.FromDateTime(DateTime.FromOADate(oaDate));

        return null;
    }

    private static string? EmptyToNull(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
