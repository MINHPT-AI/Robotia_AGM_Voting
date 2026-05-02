using Mms.Domain.Entities;

namespace Mms.Application.Interfaces;

public interface IBarQrCodeGenerator
{
    byte[] GenerateBarcode(string content);
    byte[] GenerateQrCode(string content);
    string BuildContent(string idNumber, string fullName);
}

public interface ILetterDocxBuilder
{
    byte[] BuildSingleLetterDocx(LetterBuildDto dto, byte[]? codeMarkBytes, CodeMarkType codeMarkType = CodeMarkType.None);
    byte[] BuildMergedDocx(IList<LetterBuildDto> letters, CodeMarkType codeMarkType, IBarQrCodeGenerator codeGen);

    /// <summary>
    /// Builds a letter DOCX by performing token find-replace on an uploaded template.
    /// Falls back to BuildSingleLetterDocx (synthetic) if templateBytes is null.
    /// </summary>
    byte[] BuildFromTemplate(LetterBuildDto dto, byte[] templateBytes, byte[]? codeMarkBytes, CodeMarkType codeMarkType);
}

public interface ILibreOfficePdfConverter
{
    Task<byte[]> ConvertDocxToPdfAsync(byte[] docxBytes, CancellationToken ct = default);
    Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, CancellationToken ct = default);
}

public record LetterBuildDto(
    string HoTen,
    string DiaChi,
    string DienThoai,
    string SoDKSH,
    string SoCoPhieu,
    string? TrackingCode)
{
    // Meeting-level fields for template token replacement
    public string? NgayHop { get; init; }
    public string? GioHop { get; init; }
    public string? DiaDiem { get; init; }
    public string? TenCongTy { get; init; }
    public bool IsOrganization { get; init; }           // true → [9] label = "Số ĐKKD", false → "Số CCCD"
    public IList<string>? SelectedTokens { get; init; } // tokens selected in template config, e.g. ["[2]","[7]"]
}
