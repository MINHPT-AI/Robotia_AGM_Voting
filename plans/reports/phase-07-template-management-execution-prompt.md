# Phase 07 Execution Prompt — Quản lý Mẫu Văn Bản (Template Management)

Bạn là AI thực thi Phase 07 của dự án MMS (AGM Voting System).
Dự án dùng .NET 8 / Blazor Server / MudBlazor v9 / Clean Architecture / CQRS + MediatR + FluentValidation + PostgreSQL.

**Work context:** `D:/PROJECT/Robotia_AGM_Voting`
**Plan file:** `plans/reports/phase-07-template-management-implementation-plan.md`
**Context notes:** `docs/context_style_notes.md`

Đọc plan file và context notes trước. Implement theo thứ tự 8 bước. Chạy `dotnet build` sau mỗi bước.

---

## Context nhanh

Đã có sẵn:
- `Template` entity tại `src/Mms.Domain/Entities/Template.cs` — `MeetingId` hiện NOT NULL, cần đổi nullable
- `TemplateType` enum: `Invitation`, `VotingCard`, `ElectionBallot`, `AttendanceReport`, `CountingReport`, `Minutes`
- `InvitationLetter.TemplateId` nullable FK → sẽ populate sau khi template được gán
- `LetterDocxBuilder.cs` tại `src/Mms.Infrastructure/Documents/` — hiện dùng synthetic DOCX, Phase 07 wire thêm overload đọc từ file upload
- `LibreOfficePdfConverter.cs` đã có — tái dùng cho preview PDF
- Upload infra đã có tại `wwwroot/uploads/` — templates lưu vào `wwwroot/uploads/templates/`
- NavMenu đã có link `/templates` (admin only) — trang chưa tồn tại

---

## BƯỚC 1 — Domain Changes + EF Migration

### [MODIFY] `src/Mms.Domain/Entities/Template.cs`

Thay thế toàn bộ nội dung:

```csharp
using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class Template : BaseEntity
{
    public Guid? MeetingId { get; set; }        // null = global library template
    public Meeting? Meeting { get; set; }
    public string Name { get; set; } = "";      // tên gợi nhớ do admin đặt
    public TemplateType TemplateType { get; set; }
    public string Language { get; set; } = "VN"; // VN / EN / DUAL
    public int Version { get; set; } = 1;
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }         // bytes
    public string? FieldsConfig { get; set; }   // JSON: detected + missing tokens
    public bool IsFinalized { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime? UploadedAt { get; set; }
}
```

Tạo EF Migration:

```bash
dotnet ef migrations add Phase07_TemplateSchemaUpdate \
  --project src/Mms.Infrastructure \
  --startup-project src/Mms.Web
```

---

## BƯỚC 2 — TokenRegistry Static Class

### [NEW] `src/Mms.Application/Templates/token-registry.cs`

```csharp
using Mms.Domain.Enums;

namespace Mms.Application.Templates;

/// <summary>
/// Defines fixed placeholder tokens per TemplateType.
/// Companies embed these exact tokens in their DOCX files.
/// System performs find-replace at document generation time.
/// Token format: {{TokenName}} — double curly braces, PascalCase, no spaces.
/// </summary>
public static class TokenRegistry
{
    public record TokenInfo(string Token, string Description, bool IsRequired);

    private static readonly Dictionary<TemplateType, IList<TokenInfo>> _registry = new()
    {
        [TemplateType.Invitation] =
        [
            new("{{TenCongTy}}",               "Tên công ty",                          IsRequired: true),
            new("{{HoTen}}",                   "Họ tên cổ đông",                       IsRequired: true),
            new("{{SoCoPhieu}}",               "Số cổ phiếu sở hữu",                   IsRequired: true),
            new("{{NgayHop}}",                 "Ngày họp (dd/MM/yyyy)",                IsRequired: true),
            new("{{GioHop}}",                  "Giờ họp (HH:mm)",                      IsRequired: true),
            new("{{DiaDiem}}",                 "Địa điểm tổ chức",                     IsRequired: true),
            new("{{DiaChi}}",                  "Địa chỉ cổ đông",                      IsRequired: false),
            new("{{DienThoai}}",               "Số điện thoại cổ đông",               IsRequired: false),
            new("{{SoDKSH}}",                  "Số đăng ký sở hữu",                   IsRequired: false),
        ],
        [TemplateType.VotingCard] =
        [
            new("{{TenCongTy}}",               "Tên công ty",                          IsRequired: true),
            new("{{HoTen}}",                   "Họ tên cổ đông",                       IsRequired: true),
            new("{{SoDKSH}}",                  "Số đăng ký sở hữu",                   IsRequired: true),
            new("{{SoCoPhieu}}",               "Số cổ phiếu",                          IsRequired: true),
            new("{{MaPhieu}}",                 "Mã phiếu biểu quyết",                 IsRequired: true),
            new("{{NgayHop}}",                 "Ngày họp",                             IsRequired: true),
            new("{{DanhSachNghiQuyet}}",       "Danh sách nghị quyết",                IsRequired: false),
        ],
        [TemplateType.ElectionBallot] =
        [
            new("{{TenCongTy}}",               "Tên công ty",                          IsRequired: true),
            new("{{HoTen}}",                   "Họ tên cổ đông",                       IsRequired: true),
            new("{{SoDKSH}}",                  "Số đăng ký sở hữu",                   IsRequired: true),
            new("{{SoCoPhieu}}",               "Số cổ phiếu",                          IsRequired: true),
            new("{{MaPhieu}}",                 "Mã phiếu bầu cử",                     IsRequired: true),
            new("{{NgayHop}}",                 "Ngày họp",                             IsRequired: true),
            new("{{DanhSachUngVien}}",         "Danh sách ứng viên",                  IsRequired: false),
        ],
        [TemplateType.AttendanceReport] =
        [
            new("{{TenCongTy}}",               "Tên công ty",                          IsRequired: true),
            new("{{NgayHop}}",                 "Ngày họp",                             IsRequired: true),
            new("{{DiaDiem}}",                 "Địa điểm",                             IsRequired: true),
            new("{{TongSoCDDuHop}}",           "Tổng số cổ đông dự họp",              IsRequired: true),
            new("{{TongSoCoPhieuDaiDien}}",    "Tổng số cổ phiếu đại diện",           IsRequired: true),
            new("{{TyLe}}",                    "Tỉ lệ % cổ phiếu đại diện",           IsRequired: true),
            new("{{TyLeToiThieu}}",            "Tỉ lệ % tối thiểu theo điều lệ",      IsRequired: false),
        ],
        [TemplateType.CountingReport] =
        [
            new("{{TenCongTy}}",               "Tên công ty",                          IsRequired: true),
            new("{{NgayHop}}",                 "Ngày họp",                             IsRequired: true),
            new("{{TenNghiQuyet}}",            "Tên nghị quyết/nội dung bầu cử",      IsRequired: true),
            new("{{SoPhieuTanThanh}}",         "Số phiếu tán thành",                  IsRequired: true),
            new("{{SoPhieuPhanDoi}}",          "Số phiếu phản đối",                   IsRequired: true),
            new("{{TyLeTanThanh}}",            "Tỉ lệ tán thành %",                   IsRequired: true),
            new("{{KetQua}}",                  "Kết quả (Thông qua / Không thông qua)", IsRequired: true),
            new("{{SoPhieuHopLe}}",            "Số phiếu hợp lệ",                     IsRequired: false),
            new("{{SoPhieuKhongHopLe}}",       "Số phiếu không hợp lệ",               IsRequired: false),
            new("{{SoPhieuKhongBietPhieu}}",   "Số phiếu không biểu quyết",           IsRequired: false),
        ],
        [TemplateType.Minutes] =
        [
            new("{{TenCongTy}}",               "Tên công ty",                          IsRequired: true),
            new("{{NgayHop}}",                 "Ngày họp",                             IsRequired: true),
            new("{{DiaDiem}}",                 "Địa điểm",                             IsRequired: true),
            new("{{TongSoCDDuHop}}",           "Tổng số cổ đông dự họp",              IsRequired: true),
            new("{{TongSoCoPhieuDaiDien}}",    "Tổng số cổ phiếu đại diện",           IsRequired: true),
            new("{{TyLe}}",                    "Tỉ lệ % cổ phiếu đại diện",           IsRequired: true),
            new("{{NoiDungBienBan}}",          "Nội dung biên bản",                   IsRequired: false),
        ],
    };

    public static IList<TokenInfo> GetTokens(TemplateType type)
        => _registry.TryGetValue(type, out var tokens) ? tokens : [];

    public static IList<string> GetRequiredTokens(TemplateType type)
        => GetTokens(type).Where(t => t.IsRequired).Select(t => t.Token).ToList();

    /// <summary>
    /// Scans raw text extracted from DOCX and returns which required tokens are missing.
    /// </summary>
    public static IList<string> FindMissingRequired(TemplateType type, string docxText)
    {
        var required = GetRequiredTokens(type);
        return required.Where(token => !docxText.Contains(token)).ToList();
    }
}
```

---

## BƯỚC 3 — TemplateFileService

### Interface: `src/Mms.Application/Interfaces/ITemplateFileService.cs`

```csharp
public interface ITemplateFileService
{
    Task<(string FilePath, long FileSize)> SaveAsync(Stream stream, CancellationToken ct = default);
    Task<TemplateTokenScanResult> ScanTokensAsync(string filePath, TemplateType type);
    Task<byte[]> GetDocxBytesAsync(string filePath);
    Task<byte[]> ConvertToPdfPreviewAsync(string filePath, CancellationToken ct = default);
    void Delete(string filePath);
}

public record TemplateTokenScanResult(
    IList<string> DetectedTokens,
    IList<string> MissingRequired,
    string FieldsConfigJson);
```

### [NEW] `src/Mms.Infrastructure/Documents/template-file-service.cs`

```csharp
/// <summary>
/// Handles physical file storage and token scanning for DOCX template files.
/// Files stored at wwwroot/uploads/templates/{guid}.docx
/// Token scanning uses OpenXml text extraction + Regex {{TokenName}} pattern.
/// </summary>
public class TemplateFileService : ITemplateFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILibreOfficePdfConverter _pdfConverter;

    public async Task<(string FilePath, long FileSize)> SaveAsync(Stream stream, CancellationToken ct)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "templates");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}.docx";
        var fullPath = Path.Combine(dir, fileName);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);
        return ($"uploads/templates/{fileName}", fs.Length);
    }

    public Task<TemplateTokenScanResult> ScanTokensAsync(string filePath, TemplateType type)
    {
        var fullPath = Path.Combine(_env.WebRootPath, filePath);
        var allText = ExtractDocxText(fullPath);

        // Find all {{Token}} patterns in document text
        var detected = Regex.Matches(allText, @"\{\{[A-Za-z]+\}\}")
            .Select(m => m.Value)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var missingRequired = TokenRegistry.FindMissingRequired(type, allText);

        var config = JsonSerializer.Serialize(new
        {
            detectedTokens  = detected,
            missingRequired = missingRequired,
            scannedAt       = DateTime.UtcNow
        });

        return Task.FromResult(new TemplateTokenScanResult(detected, missingRequired, config));
    }

    public async Task<byte[]> GetDocxBytesAsync(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, filePath);
        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task<byte[]> ConvertToPdfPreviewAsync(string filePath, CancellationToken ct)
    {
        var docxBytes = await GetDocxBytesAsync(filePath);
        return await _pdfConverter.ConvertDocxToPdfAsync(docxBytes, ct);
    }

    public void Delete(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, filePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    // Extract all text from DOCX using OpenXml (traverses all Text nodes)
    private static string ExtractDocxText(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body is null ? "" : string.Concat(body.Descendants<Text>().Select(t => t.Text));
    }
}
```

Register Transient trong DI.

---

## BƯỚC 4 — Application Layer

### File: `src/Mms.Application/Templates/template-dtos.cs`

```csharp
public record TemplateListItemDto(
    Guid Id, string Name, TemplateType TemplateType,
    string Language, int Version, long? FileSize,
    bool IsFinalized, DateTime? UploadedAt,
    IList<string> MissingRequiredTokens);

public record TemplateUploadResultDto(Guid Id, IList<string> MissingRequired);

public record TemplatePlaceholdersDto(
    TemplateType Type,
    IList<TokenRegistry.TokenInfo> Required,
    IList<TokenRegistry.TokenInfo> Optional);
```

---

### File: `src/Mms.Application/Templates/template-commands.cs`

**`UploadTemplateCommand`**
```csharp
public record UploadTemplateCommand(
    Stream FileStream, string OriginalFileName,
    string Name, TemplateType TemplateType,
    string Language = "VN",
    Guid? UploadedBy = null) : IRequest<TemplateUploadResultDto>;
```
Handler:
1. Validate extension: `OriginalFileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)` → throw nếu sai
2. `ITemplateFileService.SaveAsync()` → `(filePath, fileSize)`
3. `ITemplateFileService.ScanTokensAsync(filePath, TemplateType)` → `scanResult`
4. Tạo và lưu `Template` record
5. Return `new TemplateUploadResultDto(template.Id, scanResult.MissingRequired)`

**`UpdateTemplateNameCommand`**
```csharp
public record UpdateTemplateNameCommand(Guid Id, string Name, string Language) : IRequest;
```
Guard: load template → `if (template.IsFinalized) throw new BusinessException("Không thể sửa mẫu đã chốt")`

**`FinalizeTemplateCommand`**
```csharp
public record FinalizeTemplateCommand(Guid Id) : IRequest;
```
Guard: `if (template.FilePath is null) throw new BusinessException("Chưa có file — không thể chốt")`
Action: `template.IsFinalized = true; _db.SaveChangesAsync()`

**`CloneTemplateCommand`**
```csharp
public record CloneTemplateCommand(Guid SourceId, string NewName, Guid? ClonedBy = null) : IRequest<Guid>;
```
Handler:
1. Load source template
2. Copy file: `ITemplateFileService.SaveAsync(new FileStream(source.FilePath))` → new path
3. Re-scan tokens: `ScanTokensAsync(newPath, source.TemplateType)`
4. Tạo record mới: `Version = source.Version + 1`, `IsFinalized = false`, `MeetingId = null`, `Name = NewName`
5. Return new `template.Id`

**`DeleteTemplateCommand`**
```csharp
public record DeleteTemplateCommand(Guid Id) : IRequest;
```
Guard: `if (template.IsFinalized) throw new BusinessException("Không thể xóa mẫu đã chốt")`
Handler: `ITemplateFileService.Delete(template.FilePath)` + `_db.Templates.Remove(template)` + save

---

### File: `src/Mms.Application/Templates/template-queries.cs`

**`GetTemplatesQuery`**
```csharp
public record GetTemplatesQuery(
    TemplateType? FilterType = null,
    bool GlobalOnly = true) : IRequest<IList<TemplateListItemDto>>;
```
Handler:
```csharp
var q = _db.Templates.AsNoTracking()
    .Where(t => !GlobalOnly || t.MeetingId == null);
if (filterType.HasValue) q = q.Where(t => t.TemplateType == filterType);

// Deserialize MissingRequired từ FieldsConfig JSON
```

**`GetTemplatePlaceholdersQuery`**
```csharp
public record GetTemplatePlaceholdersQuery(TemplateType Type)
    : IRequest<TemplatePlaceholdersDto>;
```
Handler: chỉ đọc từ `TokenRegistry` — không cần DB:
```csharp
var tokens = TokenRegistry.GetTokens(request.Type);
return new TemplatePlaceholdersDto(
    request.Type,
    tokens.Where(t => t.IsRequired).ToList(),
    tokens.Where(t => !t.IsRequired).ToList());
```

**`PreviewTemplatePdfQuery`**
```csharp
public record PreviewTemplatePdfQuery(Guid Id) : IRequest<byte[]>;
```
Handler: load template → `ITemplateFileService.ConvertToPdfPreviewAsync(template.FilePath)`

---

### File: `src/Mms.Application/Templates/template-validators.cs`

```csharp
public class UploadTemplateValidator : AbstractValidator<UploadTemplateCommand>
{
    public UploadTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Language)
            .Must(l => new[] { "VN", "EN", "DUAL" }.Contains(l))
            .WithMessage("Ngôn ngữ phải là VN, EN hoặc DUAL");
        RuleFor(x => x.OriginalFileName)
            .Must(f => f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Chỉ chấp nhận file .docx");
    }
}

public class UpdateTemplateNameValidator : AbstractValidator<UpdateTemplateNameCommand>
{
    public UpdateTemplateNameValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Language)
            .Must(l => new[] { "VN", "EN", "DUAL" }.Contains(l));
    }
}
```

---

## BƯỚC 5 — Download API Endpoint

### [NEW] `src/Mms.Web/Api/TemplatesController.cs`

```csharp
[ApiController]
public class TemplatesController : ControllerBase
{
    [HttpGet("api/templates/{id:guid}/download")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Download(Guid id, ...)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template?.FilePath is null) return NotFound();
        var bytes = await _fileService.GetDocxBytesAsync(template.FilePath);
        var fileName = $"{template.Name}_v{template.Version}.docx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }
}
```

---

## BƯỚC 6 — Web UI

### [NEW] `src/Mms.Web/Components/Pages/Admin/template-library-page.razor`

```razor
@page "/templates"
@attribute [Authorize(Roles = "admin")]
@rendermode InteractiveServer
```

**Layout:**

```
[Header: "Thư Viện Mẫu Văn Bản"]                [Button: ➕ Tải lên mẫu mới]
──────────────────────────────────────────────────────────────────────────────
[Filter chips: Tất cả | Thư mời | Phiếu BQ | Phiếu bầu | Kiểm tra tư cách | Kiểm phiếu | Biên bản]

MudDataGrid<TemplateListItemDto>  (client-side, không cần paging vì ít records)
  Columns:
    Tên mẫu | Loại | Ngôn ngữ | Phiên bản | Kích thước | Trạng thái | Ngày upload | Hành động
```

**Status badge logic:**
```csharp
// IsFinalized = true
<MudChip Color="Color.Success" Size="Size.Small">Đã chốt</MudChip>

// IsFinalized = false + MissingRequiredTokens.Count > 0
<MudChip Color="Color.Warning" Size="Size.Small">
    Thiếu @item.MissingRequiredTokens.Count token
</MudChip>

// IsFinalized = false + MissingRequiredTokens.Count == 0
<MudChip Color="Color.Default" Size="Size.Small">Bản nháp</MudChip>
```

**Actions per row:**
```csharp
// 👁 Xem trước — tất cả trạng thái
// Click → PreviewTemplatePdfQuery → base64 PDF → JS.InvokeVoidAsync("openPdfInNewTab", base64)

// ⬇ Tải về — tất cả trạng thái
// href="/api/templates/{item.Id}/download" target="_blank"

// ✏ Sửa tên — chỉ khi !IsFinalized
// → Mở UpdateTemplateNameDialog inline (MudDialog)

// 🔒 Chốt mẫu — chỉ khi !IsFinalized
// → MudDialogService.ShowMessageBox confirm → FinalizeTemplateCommand

// 📋 Clone — tất cả trạng thái
// → Mở CloneTemplateDialog (nhập tên mới) → CloneTemplateCommand → reload

// 🗑 Xóa — chỉ khi !IsFinalized
// → MudDialogService.ShowMessageBox confirm → DeleteTemplateCommand
```

**JS helper** (thêm vào `wwwroot/js/app.js` hoặc inline `<script>` trong layout):
```javascript
window.openPdfInNewTab = (base64) => {
    const blob = new Blob(
        [Uint8Array.from(atob(base64), c => c.charCodeAt(0))],
        { type: 'application/pdf' }
    );
    window.open(URL.createObjectURL(blob), '_blank');
};
```

---

### [NEW] `src/Mms.Web/Components/Pages/Admin/Dialogs/upload-template-dialog.razor`

```razor
@* Dialog nội bộ — không có @page route *@
```

**Phần 1 — Form chọn file + metadata:**
- `MudFileUpload` accept=".docx" — trigger `OnFilesChanged`
- `MudTextField` Tên mẫu (required)
- `MudSelect<TemplateType>` Loại mẫu — khi thay đổi → load token reference
- `MudSelect<string>` Ngôn ngữ: VN / EN / DUAL

**Phần 2 — Hiện sau khi chọn file (scan preview):**
- Hiển thị: tên file + kích thước
- **Danh sách token của loại đã chọn** (từ `GetTemplatePlaceholdersQuery`):

```
Token bắt buộc:
  ✅ {{HoTen}}        — đã phát hiện trong file
  ⚠️ {{SoCoPhieu}}   — CHƯA tìm thấy trong file

Token tuỳ chọn:
  ℹ️ {{DiaChi}}       — tuỳ chọn, không bắt buộc
```

> Scan thực sự xảy ra khi Submit (trong `UploadTemplateCommand`).
> Phần 2 chỉ hiển thị danh sách token EXPECTED của loại đó để admin đối chiếu với file của mình.
> (Không cần scan client-side — giữ đơn giản.)

**Submit button:** "Tải lên" → `UploadTemplateCommand` → nếu `MissingRequired.Count > 0` hiện warning toast "Mẫu thiếu X token bắt buộc — vẫn tải lên thành công, cần bổ sung token trước khi chốt" → reload grid

---

### [NEW] `src/Mms.Web/Components/Pages/Admin/Dialogs/clone-template-dialog.razor`

- `MudTextField` "Tên mẫu mới" (pre-fill: `"{source.Name} (bản sao)"`)
- Submit → `CloneTemplateCommand` → close dialog + reload grid

---

## BƯỚC 7 — Phase 06A Integration: LetterDocxBuilder

### [MODIFY] `src/Mms.Infrastructure/Documents/LetterDocxBuilder.cs`

Thêm method mới (không xóa synthetic method cũ):

```csharp
/// <summary>
/// Builds a letter DOCX by performing token find-replace on an uploaded template.
/// Falls back to BuildSingleLetterDocx (synthetic) if templateBytes is null.
/// Tokens replaced: all {{Token}} strings in Text nodes of the document body.
/// </summary>
public byte[] BuildFromTemplate(LetterBuildDto dto, byte[] templateBytes, byte[]? codeMarkBytes, CodeMarkType codeMarkType)
{
    using var ms = new MemoryStream();
    ms.Write(templateBytes, 0, templateBytes.Length);
    ms.Position = 0;

    using var doc = WordprocessingDocument.Open(ms, true);
    var body = doc.MainDocumentPart!.Document.Body!;

    // Build replacement map from LetterBuildDto
    var replacements = new Dictionary<string, string>
    {
        ["{{HoTen}}"]      = dto.HoTen,
        ["{{DiaChi}}"]     = dto.DiaChi ?? "",
        ["{{DienThoai}}"]  = dto.DienThoai ?? "",
        ["{{SoDKSH}}"]     = dto.SoDKSH,
        ["{{SoCoPhieu}}"]  = dto.SoCoPhieu,
        // Meeting-level tokens phải được caller truyền vào LetterBuildDto
        ["{{NgayHop}}"]    = dto.NgayHop ?? "",
        ["{{GioHop}}"]     = dto.GioHop ?? "",
        ["{{DiaDiem}}"]    = dto.DiaDiem ?? "",
        ["{{TenCongTy}}"]  = dto.TenCongTy ?? "",
    };

    // Replace all Text nodes
    foreach (var text in body.Descendants<Text>())
    {
        foreach (var (token, value) in replacements)
            text.Text = text.Text.Replace(token, value);
    }

    // Insert barcode at BARCODE_MARK bookmark if provided
    if (codeMarkBytes is { Length: > 0 })
        InsertImageAtBookmark(doc.MainDocumentPart, body, "BARCODE_MARK", codeMarkBytes, codeMarkType);

    doc.MainDocumentPart.Document.Save();
    return ms.ToArray();
}
```

Cập nhật `LetterBuildDto` thêm các field meeting-level:
```csharp
// Thêm vào LetterBuildDto:
public string? NgayHop { get; init; }
public string? GioHop { get; init; }
public string? DiaDiem { get; init; }
public string? TenCongTy { get; init; }
```

### [MODIFY] Handler `GenerateLettersCommand` (hoặc `ExportLettersDocxCommand`)

Thêm fallback template lookup:

```csharp
// 1. Tìm template Invitation finalized cho meeting cụ thể
var template = await _db.Templates
    .Where(t => t.MeetingId == meetingId
             && t.TemplateType == TemplateType.Invitation
             && t.IsFinalized)
    .FirstOrDefaultAsync(ct);

// 2. Fallback: global finalized template
template ??= await _db.Templates
    .Where(t => t.MeetingId == null
             && t.TemplateType == TemplateType.Invitation
             && t.IsFinalized)
    .FirstOrDefaultAsync(ct);

// 3. Build letter
byte[] docxBytes;
if (template?.FilePath is not null)
{
    var templateBytes = await _fileService.GetDocxBytesAsync(template.FilePath);
    docxBytes = _docxBuilder.BuildFromTemplate(dto, templateBytes, codeMarkBytes, codeMarkType);
    letter.TemplateId = template.Id;  // ghi lại template đã dùng
}
else
{
    // Fallback về synthetic
    docxBytes = _docxBuilder.BuildSingleLetterDocx(dto, codeMarkBytes, codeMarkType);
}
```

---

## BƯỚC 8 — Build Verify + Smoke Test

```bash
dotnet build --configuration Release
dotnet test tests/Mms.UnitTests/ --configuration Release
dotnet test tests/Mms.IntegrationTests/ --configuration Release
```

**Manual smoke test (docker-compose up -d):**
1. `/templates` → upload DOCX có đủ token Invitation → badge "Bản nháp"
2. Upload DOCX thiếu `{{SoCoPhieu}}` → badge "Thiếu 1 token", warning toast
3. Xem trước → PDF mở tab mới
4. Tải về → DOCX download đúng tên
5. Chốt mẫu → badge "Đã chốt", ẩn nút Sửa/Xóa
6. Thử xóa template đã chốt → thông báo lỗi
7. Clone → dialog nhập tên → bản sao Version 2 xuất hiện trong grid
8. `/meetings/{id}/letters` → Xuất DOCX → kiểm tra file dùng đúng template đã upload
9. Update `docs/context_style_notes.md` thêm Phase 07 section

---

## Checklist hoàn thành

- [ ] Migration `Phase07_TemplateSchemaUpdate` chạy clean
- [ ] `dotnet build` → 0 errors
- [ ] Upload DOCX đầy đủ token → scan đúng, không MissingRequired
- [ ] Upload DOCX thiếu token → MissingRequired list đúng
- [ ] Preview PDF hoạt động qua LibreOffice
- [ ] Download DOCX đúng file
- [ ] Finalize → lock (không sửa/xóa được)
- [ ] Clone → Version+1, IsFinalized=false
- [ ] Phase 06A: generate letters dùng template đã upload
- [ ] `docs/context_style_notes.md` updated

**Report cuối:** `DONE` hoặc `DONE_WITH_CONCERNS` theo orchestration protocol.

> **Không viết unit test trong Phase 07** — coverage bổ sung ở phase sau.
