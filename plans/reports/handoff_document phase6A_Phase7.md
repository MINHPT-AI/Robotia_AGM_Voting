# Handoff: Sửa Tính Năng Tạo Thư Mời Hàng Loạt — Robotia AGM Voting

> **Ngày**: 2026-04-26
> **Trạng thái**: ⚠️ Code đã sửa xong, build thành công, unit test passed — nhưng **chưa hoạt động đúng trên production** do template thực tế không chứa token.
> **Workspace**: `d:\PROJECT\Robotia_AGM_Voting`

---

## 1. BỐI CẢNH DỰ ÁN

**Stack**: .NET 8 / Blazor Server / MudBlazor v9 / Clean Architecture / EF Core 8 / PostgreSQL 16 / Docker Compose (3 services: blazor-app, postgres, libreoffice)

**Tính năng**: Tạo thư mời cá nhân hóa cho cổ đông (invitation letters) từ template DOCX, xuất DOCX/PDF hàng loạt.

**Token format chuẩn**: `[N]` — ví dụ `[1]` = Tên công ty, `[2]` = Họ tên CĐ, `[7]` = Địa chỉ, `[8]` = Điện thoại, `[9]` = Số CCCD/ĐKKD.

---

## 2. CÁC VẤN ĐỀ ĐÃ ĐƯỢC SỬA (CODE ĐÃ COMMIT)

### Vấn đề 1: Token format mismatch
- **Trước**: `BuildFromTemplate()` dùng `{{HoTen}}`, `{{DiaChi}}`...
- **Sau**: Đổi sang `[1]`, `[2]`,..., `[9]` format, khớp với `TokenRegistry` và UI.
- **File**: `LetterDocxBuilder.cs` L214-280

### Vấn đề 2: Split-run token không được replace
- **Vấn đề**: Word có thể cắt `[2]` thành 3 runs: `[` + `2` + `]` → simple text replace thất bại.
- **Giải pháp**: 2-pass approach:
  1. First pass: direct replacement trên từng `<w:t>` element
  2. Second pass: nếu còn token → merge tất cả runs → replace → rebuild (giữ `<w:br/>`)
- **Các hàm mới (internal static)**:
  - `ReplaceTokensInBody()` — orchestrator
  - `MergeRunsAndReplace()` — xử lý split-run
  - `RemoveMarkedLines()` — xóa dòng unselected token
- **Unit tests**: 6/6 passed tại `tests\Mms.UnitTests\Documents\LetterDocxBuilderTokenTests.cs`

### Vấn đề 3: Token `[9]` unselected → dòng trống xấu
- **Trước**: Replace bằng `""` → "Số CCCD/ĐKKD: " hiện rỗng
- **Sau**: Dùng sentinel `\u0000__REMOVE_LINE__\u0000` → xóa cả run chứa token + `<w:br/>` trước nó
- **File**: `LetterDocxBuilder.cs`, hàm `RemoveMarkedLines()`

### Vấn đề 4: GenerateLetters return 0 gây nhầm lẫn
- **Trước**: `shareholders.Count == 0` → return 0 → UI hiện "Không có thư mới"
- **Sau**: throw `InvalidOperationException("Chưa có danh sách cổ đông...")` → UI catch và hiện warning (orange)
- **Thêm**: `_trackingGrid?.ReloadServerData()` sau generate → grid refresh ngay
- **Files**: `LetterHandlers.cs` L29-76, `InvitationLettersPage.razor` L426-446

### Vấn đề 5: ExportPdf không dùng template
- **Trước**: `ExportLettersPdfHandler` chỉ gọi `BuildMergedDocx()` (synthetic), bỏ qua template lookup
- **Sau**: Copy logic template lookup từ DOCX handler (meeting-specific → global → synthetic fallback)
- **Thêm**: Inject `ITemplateFileService`, populate `IsOrganization` + `SelectedTokens` vào DTO
- **File**: `LetterHandlers.cs` L210-282

### Vấn đề 6: FinalizeTemplate guard quá chặt
- **Trước**: Chỉ check `HtmlContent` → template chỉ có DOCX file → không finalize được
- **Sau**: Check `FilePath || HtmlContent`
- **File**: `TemplateHandlers.cs` L106

### Vấn đề 7: AltChunk merge code trùng lặp
- **Trước**: Inline AltChunk merge trong `ExportLettersDocxHandler`
- **Sau**: Extract ra `LetterDocxBuilder.MergeDocxFiles()` static method, dùng chung cho cả DOCX và PDF
- **File**: `LetterDocxBuilder.cs` L492-525

---

## 3. DANH SÁCH FILE ĐÃ SỬA

| # | File | Thay đổi |
|---|------|----------|
| 1 | `src/Mms.Application/Interfaces/ILetterServices.cs` | +`IsOrganization`, +`SelectedTokens` vào `LetterBuildDto` |
| 2 | `src/Mms.Infrastructure/Documents/LetterDocxBuilder.cs` | Rewrite `BuildFromTemplate`, +4 hàm mới, +`MergeDocxFiles`, +`InsertImageAtBookmark` (re-add) |
| 3 | `src/Mms.Infrastructure/Handlers/InvitationLetters/LetterHandlers.cs` | Fix GenerateLetters, ExportDocx, **rewrite ExportPdf** |
| 4 | `src/Mms.Web/Components/Pages/Meetings/InvitationLettersPage.razor` | Catch InvalidOperationException + `_trackingGrid?.ReloadServerData()` |
| 5 | `src/Mms.Infrastructure/Handlers/Templates/TemplateHandlers.cs` | FinalizeTemplate guard relaxed |
| 6 | `src/Mms.Infrastructure/Mms.Infrastructure.csproj` | +`InternalsVisibleTo` cho UnitTests |
| 7 | `tests/Mms.UnitTests/Mms.UnitTests.csproj` | +`DocumentFormat.OpenXml` 3.1.1 |
| 8 | `tests/Mms.UnitTests/Documents/LetterDocxBuilderTokenTests.cs` | **[NEW]** 6 unit tests |

---

## 4. KẾT QUẢ KIỂM TRA

| Kiểm tra | Kết quả |
|----------|---------|
| `dotnet build Mms.sln` | ✅ 0 errors, 0 warnings |
| Unit tests (6 tests) | ✅ All passed |
| Docker build & deploy | ✅ 3 containers running |
| **Smoke test trên browser** | ❌ **THẤT BẠI** — xem mục 5 |

---

## 5. VẤN ĐỀ ĐANG VƯỚNG (CHƯA GIẢI QUYẾT)

### 5.1 Root cause: Template thực tế KHÔNG chứa token `[N]`

**Phát hiện qua điều tra**:
- Template finalized trong DB: `1bd57b20-862e-4bb3-a0c4-7e20d645bc18`
- File: `uploads/templates/177afc28148649c0ac7aa0962724c2d6.docx`
- SelectedTokens trong DB: `["[2]","[3]","[9]","[8]","[7]"]`
- **Nhưng khi extract XML**: file DOCX này là **thông báo mời họp chung** của công ty (hardcoded toàn bộ nội dung), **KHÔNG có bất kỳ token `[2]`, `[7]`, `[8]`, `[9]` nào bên trong**.

**Hậu quả**:
- `ReplaceTokensInBody()` chạy nhưng không tìm thấy token → không replace gì
- Output = template gốc lặp lại cho mọi cổ đông (không cá nhân hóa)
- Screenshot từ user cho thấy thư có "Kính gửi: Lê Đức Anh", "Địa chỉ: 617..." → đây là output từ **synthetic builder** (fallback `BuildMergedDocx`), KHÔNG phải từ template
- Có vẻ user đã export thư **trước** khi deploy bản mới, hoặc flow đang dùng synthetic vì lý do khác

### 5.2 Giải pháp cần thực hiện (CHƯA LÀM)

Có 2 hướng (cần xác nhận với user):

**Hướng A — Tạo template mẫu đúng chuẩn**:
- Tạo file DOCX có chứa tokens `[2]`, `[7]`, `[8]`, `[9]` ở vị trí phù hợp
- Upload lên hệ thống, finalize → test lại
- Ưu: Đúng thiết kế, template đúng format sẽ hoạt động
- Nhược: User cần hiểu cách tạo template

**Hướng B — Smart fallback khi template không có token**:
- Trong `ExportLettersDocxHandler`: sau khi load template, **kiểm tra xem template có chứa ít nhất 1 token không**
- Nếu không có token nào → fallback sang synthetic builder + log warning
- Code mẫu:
```csharp
// After loading templateBytes, check for tokens
var hasTokens = false;
using (var checkDoc = WordprocessingDocument.Open(new MemoryStream(templateBytes), false))
{
    var bodyText = checkDoc.MainDocumentPart!.Document.Body!.InnerText;
    hasTokens = selectedTokens?.Any(t => bodyText.Contains(t)) ?? false;
}
if (!hasTokens)
{
    _logger.LogWarning("Template {Id} has no tokens, falling back to synthetic", template.Id);
    templateBytes = null; // triggers synthetic fallback
}
```

**Hướng C — Hybrid (recommended)**:
- Implement cả A + B: smart fallback + cung cấp template mẫu

### 5.3 Screenshot vấn đề từ user
User gửi screenshot cho thấy output formatting không đúng — các field "Kính gửi:", "Địa chỉ:", "Điện thoại:", "Số ĐKSH:" xuất hiện với:
- Bold, centered/indented bất thường
- Spacing quá rộng giữa các dòng
- Đây là layout của synthetic builder (`BuildMergedDocx`), cho thấy template path KHÔNG được dùng thực tế

---

## 6. CÁCH TIẾP TỤC CÔNG VIỆC

### Bước ngay lập tức:
1. **Xác nhận với user** muốn hướng A, B hay C
2. Nếu B/C: thêm token-check logic vào `ExportLettersDocxHandler` và `ExportLettersPdfHandler`
3. Nếu A/C: tạo template DOCX mẫu có token `[2]`, `[7]`, `[8]`, `[9]` → upload → finalize → test

### Smoke test plan (6 bước):
1. Tạo meeting + import danh sách CĐ
2. Upload template có token → finalize
3. Vào Thư mời → Tạo danh sách thư → kiểm tra snackbar + grid refresh
4. Xuất DOCX → mở file → kiểm tra token replaced + formatting
5. Xuất PDF → kiểm tra PDF tương tự
6. Xóa tất cả thư → tạo lại → kiểm tra count

---

## 7. THÔNG TIN KỸ THUẬT BỔ SUNG

### DB Connection (Docker)
```bash
docker exec robotia_agm_voting-postgres-1 psql -U mms -d mms
```
> ⚠️ PowerShell quoting issue: dùng file .sql pipe vào `docker exec -i` thay vì inline query

### Template data hiện tại trong DB
```
Id: 1bd57b20 | Name: "Thông báo mời họp đại hội cổ đông" | IsFinalized: true
FilePath: uploads/templates/177afc28148649c0ac7aa0962724c2d6.docx
SelectedTokens: ["[2]","[3]","[9]","[8]","[7]"]
MeetingId: NULL (global template)
```

### Key file paths
- **Builder**: `src/Mms.Infrastructure/Documents/LetterDocxBuilder.cs`
- **Handlers**: `src/Mms.Infrastructure/Handlers/InvitationLetters/LetterHandlers.cs`
- **UI**: `src/Mms.Web/Components/Pages/Meetings/InvitationLettersPage.razor`
- **Template entity**: `src/Mms.Domain/Entities/Template.cs`
- **Template service**: `src/Mms.Infrastructure/Documents/TemplateFileService.cs`
- **Unit tests**: `tests/Mms.UnitTests/Documents/LetterDocxBuilderTokenTests.cs`

### Docker
```bash
docker-compose up -d --build    # rebuild & start
docker logs robotia_agm_voting-blazor-app-1 --tail 100   # check logs
```

### Key interfaces
- `ILetterDocxBuilder` — builds single/merged DOCX
- `ITemplateFileService` — reads template bytes from disk
- `IBarQrCodeGenerator` — generates barcode/QR
- `ILibreOfficePdfConverter` — DOCX → PDF via LibreOffice headless

### Template.SelectedTokens format
JSON string array in DB: `["[2]","[7]","[8]"]`
Read with: `JsonSerializer.Deserialize<List<string>>(template.SelectedTokens)`
