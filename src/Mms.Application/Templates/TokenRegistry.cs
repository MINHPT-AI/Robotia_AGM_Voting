namespace Mms.Application.Templates;

/// <summary>
/// Flat token registry — all tokens are optional, user picks which ones to use.
/// Token codes use numeric format [1], [2], [3]... for easy insertion by users.
/// In the WYSIWYG editor, tokens display as styled chips: "[1] Tên công ty"
/// At export time, system replaces token codes with actual data.
/// </summary>
public static class TokenRegistry
{
    public record TokenInfo(string Code, string Description, string DataSource);

    private static readonly IList<TokenInfo> _allTokens =
    [
        new("[1]",  "Tên công ty",          "Company.Name"),
        new("[2]",  "Họ tên cổ đông",       "Shareholder.Name"),
        new("[3]",  "Số cổ phiếu",          "Shareholder.SharesTotal"),
        new("[4]",  "Ngày họp",             "Meeting.MeetingDate (dd/MM/yyyy)"),
        new("[5]",  "Giờ họp",              "Meeting.MeetingDate (HH:mm)"),
        new("[6]",  "Địa điểm",             "Meeting.Location"),
        new("[7]",  "Địa chỉ cổ đông",      "Shareholder.Address"),
        new("[8]",  "Điện thoại cổ đông",   "Shareholder.Phone"),
        new("[9]",  "Số ĐKSH",              "Shareholder.IdNumber"),
        new("[10]", "Mã chứng khoán",       "Company.StockCode"),
        new("[11]", "Người đại diện",       "Company.LegalRepName"),
        new("[12]", "Chức vụ người ĐD",     "Company.LegalRepTitle"),
        new("[13]", "Mã số thuế",           "Company.TaxCode"),
        new("[14]", "Vốn điều lệ",          "Company.CharterCapital"),
        new("[15]", "Tổng CP phát hành",    "Company.TotalSharesIssued"),
    ];

    /// <summary>Returns the full list of available tokens.</summary>
    public static IList<TokenInfo> GetAllTokens() => _allTokens;

    /// <summary>Filters tokens by selected codes, e.g. ["[1]","[2]","[5]"].</summary>
    public static IList<TokenInfo> GetTokensByCode(IEnumerable<string> codes)
    {
        var codeSet = codes.ToHashSet();
        return _allTokens.Where(t => codeSet.Contains(t.Code)).ToList();
    }

    /// <summary>
    /// Replaces all token codes in HTML content with actual values.
    /// Token spans: <span class="mce-token" data-token="[1]" ...>[1] Tên công ty</span>
    /// Replaced with plain text values.
    /// </summary>
    public static string ReplaceTokensInHtml(string html, Dictionary<string, string> tokenValues)
    {
        foreach (var (code, value) in tokenValues)
        {
            // Replace styled token spans with the actual value
            // Pattern: <span ...data-token="[X]"...>any text</span>
            var pattern = $@"<span[^>]*data-token=""{System.Text.RegularExpressions.Regex.Escape(code)}""[^>]*>[^<]*</span>";
            html = System.Text.RegularExpressions.Regex.Replace(html, pattern, value);

            // Also replace raw token codes (if user typed them directly)
            html = html.Replace(code, value);
        }
        return html;
    }
}
