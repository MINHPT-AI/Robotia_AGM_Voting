# Phase 07 — Quản lý Mẫu Văn Bản (Template Management)

## Quyết định thiết kế (đã thống nhất)

| # | Quyết định |
|---|---|
| Q1 | `MeetingId` nullable → có thư viện template toàn hệ thống (global) + per-meeting override sau |
| Q2 | Upload/quản lý cả 6 TemplateType ngay, fill data làm dần từng phase |
| Q3 | **Cố định tên token** per TemplateType — vị trí/layout/nội dung xung quanh do công ty tự thiết kế trong DOCX |
| Q4 | Chỉ nhận `.docx`. Preview → LibreOffice → PDF on-demand. Download → DOCX gốc |
| Q5 | `IsFinalized` = admin bấm "Chốt mẫu" thủ công → lock, có thể clone |

---

## Hiện trạng code cần sửa

| File | Thay đổi |
|---|---|
| `Template.cs` | `MeetingId` → nullable, thêm `Name`, `FileSize` |
| `LetterDocxBuilder.cs` | Hiện dùng synthetic → Phase 07 wire đọc từ file DOCX upload |
| `InvitationLetter.TemplateId` | Đã có FK nullable → Phase 07 sẽ populate khi generate |

---

## Token Registry — Cố định per TemplateType

### `src/Mms.Application/Templates/token-registry.cs`

Static class định nghĩa bộ token cho mỗi loại:

```
TemplateType.Invitation (Thư mời họp):
  Bắt buộc: {{HoTen}}, {{SoCoPhieu}}, {{NgayHop}}, {{GioHop}}, {{DiaDiem}}, {{TenCongTy}}
  Tuỳ chọn: {{DiaChi}}, {{DienThoai}}, {{SoDKSH}}

TemplateType.VotingCard (Phiếu biểu quyết):
  Bắt buộc: {{HoTen}}, {{SoDKSH}}, {{SoCoPhieu}}, {{MaPhieu}}, {{NgayHop}}, {{TenCongTy}}
  Tuỳ chọn: {{DanhSachNghiQuyet}}

TemplateType.ElectionBallot (Phiếu bầu cử):
  Bắt buộc: {{HoTen}}, {{SoDKSH}}, {{SoCoPhieu}}, {{MaPhieu}}, {{NgayHop}}, {{TenCongTy}}
  Tuỳ chọn: {{DanhSachUngVien}}

TemplateType.AttendanceReport (Báo cáo kiểm tra tư cách):
  Bắt buộc: {{NgayHop}}, {{DiaDiem}}, {{TenCongTy}}, {{TongSoCDDuHop}}, {{TongSoCoPhieuDaiDien}}, {{TyLe}}
  Tuỳ chọn: {{TyLeToiThieu}}

TemplateType.CountingReport (Biên bản kiểm phiếu):
  Bắt buộc: {{NgayHop}}, {{TenCongTy}}, {{TenNghiQuyet}}, {{SoPhieuTanThanh}}, {{SoPhieuPhanDoi}}, {{TyLeTanThanh}}, {{KetQua}}
  Tuỳ chọn: {{SoPhieuHopLe}}, {{SoPhieuKhongHopLe}}, {{SoPhieuKhongBietPhieu}}

TemplateType.Minutes (Biên bản/Nghị quyết ĐHCĐ):
  Bắt buộc: {{NgayHop}}, {{DiaDiem}}, {{TenCongTy}}, {{TongSoCDDuHop}}, {{TongSoCoPhieuDaiDien}}, {{TyLe}}
  Tuỳ chọn: {{NoiDungBienBan}}
```

Token chung (dùng được ở mọi type):
- `{{TenCongTy}}` → Company.Name
- `{{NgayHop}}` → Meeting.MeetingDate (dd/MM/yyyy)
- `{{GioHop}}` → Meeting.MeetingDate (HH:mm)
- `{{DiaDiem}}` → Meeting.Location

---

## Entity Changes

### [MODIFY] `src/Mms.Domain/Entities/Template.cs`

```csharp
public class Template : BaseEntity
{
    public Guid? MeetingId { get; set; }       // null = global library
    public Meeting? Meeting { get; set; }
    public string Name { get; set; } = "";     // NEW: tên gợi nhớ
    public TemplateType TemplateType { get; set; }
    public string Language { get; set; } = "VN";
    public int Version { get; set; } = 1;
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }        // NEW: bytes
    public string? FieldsConfig { get; set; }  // JSON: detected + missing tokens
    public bool IsFinalized { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime? UploadedAt { get; set; }
}
```

**Migration:** `Phase07_TemplateSchemaUpdate`
- `MeetingId` → nullable
- Add column `Name nvarchar(200) not null default ''`
- Add column `FileSize bigint null`

---

## Infrastructure Service

### `src/Mms.Infrastructure/Documents/template-file-service.cs`

Interface `ITemplateFileService` ở Application layer.

Methods:
- `Task<(string filePath, long fileSize)> SaveAsync(Stream stream, string originalFileName)` → lưu vào `wwwroot/uploads/templates/{newGuid}.docx`
- `Task<TemplateTokenScanResult> ScanTokensAsync(string filePath)` → mở DOCX bằng OpenXml, extract text, Regex `\{\{[A-Za-z]+\}\}`, trả list found + list missingRequired
- `Task<byte[]> GetDocxBytesAsync(string filePath)` → đọc file về byte[]
- `Task<byte[]> ConvertToPdfAsync(string filePath)` → gọi LibreOffice headless (tái dùng `LibreOfficePdfConverter`)
- `void Delete(string filePath)` → xóa file vật lý

```csharp
public record TemplateTokenScanResult(
    IList<string> DetectedTokens,
    IList<string> MissingRequired,
    string FieldsConfigJson   // JSON serialized để lưu vào DB
);
```

---

## Application Layer

### Thư mục: `src/Mms.Application/Templates/`

#### Commands (`template-commands.cs`)

**`UploadTemplateCommand`**
```csharp
record UploadTemplateCommand(
    Stream FileStream, string OriginalFileName,
    string Name, TemplateType TemplateType,
    string Language = "VN") : IRequest<TemplateUploadResultDto>;
```
Handler:
1. Validate: extension `.docx` only, size ≤ 20MB
2. `ITemplateFileService.SaveAsync()` → get filePath + fileSize
3. `ITemplateFileService.ScanTokensAsync()` → get TokenScanResult
4. Tạo `Template` record, lưu DB
5. Trả `TemplateUploadResultDto { Id, MissingRequired }` (warning nếu thiếu token bắt buộc)

**`UpdateTemplateNameCommand`**
```csharp
record UpdateTemplateNameCommand(Guid Id, string Name, string Language) : IRequest;
```
Guard: không được update nếu `IsFinalized = true`

**`FinalizeTemplateCommand`**
```csharp
record FinalizeTemplateCommand(Guid Id) : IRequest;
```
Guard: phải có FilePath (đã upload file). Set `IsFinalized = true`.

**`CloneTemplateCommand`**
```csharp
record CloneTemplateCommand(Guid SourceId, string NewName) : IRequest<Guid>;
```
Handler: copy file vật lý (new guid filename) + tạo record mới với `Version = source.Version + 1`, `IsFinalized = false`, `MeetingId = null`.

**`DeleteTemplateCommand`**
```csharp
record DeleteTemplateCommand(Guid Id) : IRequest;
```
Guard: `IsFinalized = true` → throw BusinessException "Không thể xóa mẫu đã chốt".
Handler: `ITemplateFileService.Delete()` + xóa DB record.

#### Queries (`template-queries.cs`)

**`GetTemplatesQuery`**
```csharp
record GetTemplatesQuery(
    TemplateType? FilterType = null,
    bool GlobalOnly = true) : IRequest<IList<TemplateListItemDto>>;
```

**`GetTemplatePlaceholdersQuery`**
```csharp
record GetTemplatePlaceholdersQuery(TemplateType Type)
    : IRequest<TemplatePlaceholdersDto>;
```
Handler: trả từ `TokenRegistry` (không cần DB).

**`PreviewTemplatePdfQuery`**
```csharp
record PreviewTemplatePdfQuery(Guid Id) : IRequest<byte[]>;
```
Handler: đọc file DOCX → `ITemplateFileService.ConvertToPdfAsync()` → trả PDF bytes.

#### DTOs (`template-dtos.cs`)

```csharp
record TemplateListItemDto(
    Guid Id, string Name, TemplateType TemplateType,
    string Language, int Version, long? FileSize,
    bool IsFinalized, DateTime? UploadedAt,
    IList<string> MissingRequiredTokens);

record TemplateUploadResultDto(Guid Id, IList<string> MissingRequired);

record TemplatePlaceholdersDto(
    TemplateType Type,
    IList<TokenInfo> Required,
    IList<TokenInfo> Optional);

record TokenInfo(string Token, string Description);
```

#### Validators (`template-validators.cs`)

```csharp
// UploadTemplateValidator
RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
RuleFor(x => x.Language).Must(l => new[]{"VN","EN","DUAL"}.Contains(l));
// File validation in handler (extension + size)

// UpdateTemplateNameValidator
RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
```

---

## Web UI

### [NEW] `src/Mms.Web/Components/Pages/Admin/template-library-page.razor`
Route: `@page "/templates"` + `@attribute [Authorize(Roles = "admin")]`

**Layout:**

```
[Header: "Thư Viện Mẫu Văn Bản"]          [Button: ➕ Tải lên mẫu mới]
──────────────────────────────────────────────────────────────────────
[Filter chips: Tất cả | Thư mời | Phiếu BQ | Phiếu bầu | Báo cáo | Biên bản | Nghị quyết]

MudDataGrid<TemplateListItemDto>
  Columns:
    Tên mẫu | Loại | Ngôn ngữ | Phiên bản | Kích thước | Trạng thái | Ngày upload | Hành động
```

**Status badge:**
- `IsFinalized = true` → `MudChip Color="Success"` "Đã chốt"
- `IsFinalized = false` + `MissingRequired.Count > 0` → `MudChip Color="Warning"` "Thiếu token"
- `IsFinalized = false` + `MissingRequired.Count = 0` → `MudChip Color="Default"` "Bản nháp"

**Actions per row (icon buttons):**
- 👁 **Xem trước** → call `PreviewTemplatePdfQuery` → mở PDF trong tab mới (`data:application/pdf;base64,...`)
- ⬇ **Tải về** → GET `/api/templates/{id}/download` → stream DOCX
- ✏ **Sửa tên** → chỉ hiển thị nếu `!IsFinalized` → `UpdateTemplateNameDialog`
- 🔒 **Chốt mẫu** → chỉ hiển thị nếu `!IsFinalized` → confirm dialog → `FinalizeTemplateCommand`
- 📋 **Clone** → `CloneTemplateDialog` (nhập tên mới) → `CloneTemplateCommand`
- 🗑 **Xóa** → chỉ hiển thị nếu `!IsFinalized` → confirm → `DeleteTemplateCommand`

---

### Dialog: `upload-template-dialog.razor`

3 bước trong 1 dialog (không dùng Stepper — đơn giản):

**Phần 1 — Chọn file:**
- `MudFileUpload` accept `.docx` only
- `MudTextField` Tên mẫu (required)
- `MudSelect<TemplateType>` Loại mẫu
- `MudSelect<string>` Ngôn ngữ (VN / EN / DUAL)

**Phần 2 — Hiển thị sau khi chọn file (scan result):**
- Tên file + kích thước
- List ✅ token phát hiện được
- List ⚠️ token bắt buộc còn thiếu (warning, không block upload)
- Tooltip: "Bạn cần đặt đúng tên token trong file DOCX. Xem danh sách token →" [link mở panel hướng dẫn]

**Token Reference Panel** (MudExpansionPanel hoặc MudDrawer phụ):
- Hiển thị bảng token của TemplateType đang chọn
- Token | Mô tả | Bắt buộc/Tuỳ chọn

**Submit** → `UploadTemplateCommand` → toast success + reload grid

---

### Minimal API Endpoint

`src/Mms.Web/Api/TemplatesController.cs`

```
GET /api/templates/{id}/download
    → Results.File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName)
```

---

## Phase 06A Integration

### [MODIFY] `src/Mms.Infrastructure/Documents/LetterDocxBuilder.cs`

Thêm overload nhận `byte[] templateDocxBytes` (đọc từ DB):

```csharp
// Hiện tại: BuildSingleLetterDocx(dto, codeMarkBytes, codeMarkType)
// Thêm: BuildSingleLetterDocxFromTemplate(dto, templateBytes, codeMarkBytes, codeMarkType)
```

Logic `BuildSingleLetterDocxFromTemplate`:
- Mở templateBytes bằng OpenXml
- Find-replace tất cả token `{{...}}` trong tất cả `Text` nodes
- Insert barcode tại bookmark `BARCODE_MARK` (nếu có)
- Trả DOCX bytes đã fill

### [MODIFY] Handler `GenerateLettersCommand` (Phase 06A)

Thêm fallback logic:
1. Tìm template `TemplateType.Invitation` finalized, `MeetingId = thisMeetingId`
2. Fallback: tìm global (`MeetingId = null`), finalized, type Invitation
3. Fallback: dùng synthetic (giữ nguyên behavior cũ)

---

## Execution Order (8 Bước)

| Bước | Nội dung | Est. Time |
|------|----------|-----------|
| 1 | Domain changes + EF Migration | 0.5h |
| 2 | `TokenRegistry` static class (6 types × required + optional) | 0.5h |
| 3 | `TemplateFileService` (save, scan, read, convert, delete) | 1.5h |
| 4 | Application layer: 5 Commands + 3 Queries + Validators + DTOs | 2h |
| 5 | `TemplatesController` download endpoint | 0.5h |
| 6 | `template-library-page.razor` + `upload-template-dialog.razor` | 2h |
| 7 | Phase 06A integration: LetterDocxBuilder template-aware | 1h |
| 8 | Build verify + smoke test | 0.5h |

**Tổng ước tính: ~8.5h** (~1 ngày)

---

## Verification Checklist

- [ ] Migration chạy clean, `MeetingId` nullable, `Name` + `FileSize` tồn tại
- [ ] Upload DOCX có đủ token → grid hiện "Bản nháp", MissingRequired = empty
- [ ] Upload DOCX thiếu token bắt buộc → badge "Thiếu token" + warning hiện đúng
- [ ] Preview → PDF mở được trong tab mới
- [ ] Download → file DOCX tải về đúng
- [ ] Chốt mẫu → badge "Đã chốt", ẩn nút Sửa/Xóa
- [ ] Xóa template đã chốt → báo lỗi
- [ ] Clone → tạo record mới Version+1, IsFinalized=false
- [ ] Phase 06A: generate letters với template đã upload → DOCX dùng đúng mẫu
- [ ] `dotnet build` → 0 errors
- [ ] Update `docs/context_style_notes.md`
