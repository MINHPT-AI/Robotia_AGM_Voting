# Phase 06A Execution Prompt — Gửi Thư Mời Giấy (Physical Invitation Letter Management)

Bạn là AI thực thi Phase 06A của dự án MMS (AGM Voting System).
Dự án dùng .NET 8 / Blazor Server / MudBlazor v9 / Clean Architecture / CQRS + MediatR + FluentValidation + PostgreSQL.

**Work context:** `D:/PROJECT/Robotia_AGM_Voting`
**Plan file:** `plans/reports/phase-06a-invitation-letters-physical-mailing-implementation-plan.md`
**Context notes:** `docs/context_style_notes.md`

Đọc plan file trước. Sau đó đọc `context_style_notes.md` để nắm code conventions.
Implement theo thứ tự 9 bước bên dưới. Sau mỗi file mới, chạy `dotnet build` để xác nhận compile sạch.

---

## BƯỚC 1 — Domain Entity + EF Migration

### File mới: `src/Mms.Domain/Entities/InvitationLetter.cs`

```csharp
public class InvitationLetter
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string ShareholderIdNumber { get; set; } = "";
    public string ShareholderName { get; set; } = "";
    public string? ShareholderAddress { get; set; }
    public string? ShareholderPhone { get; set; }
    public long VotingRights { get; set; }
    public long SharesTotal { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.NotSent;
    public string? TrackingCode { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
    public string? FailureReason { get; set; }
    public CodeMarkType CodeMarkType { get; set; } = CodeMarkType.Barcode;
    public Guid? TemplateId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Meeting Meeting { get; set; } = null!;
}

public enum InvitationStatus { NotSent = 0, Dispatched = 1, Delivered = 2, Failed = 3, Returned = 4 }
public enum CodeMarkType { None, Barcode, QRCode }
```

Thêm `DbSet<InvitationLetter> InvitationLetters` vào `AppDbContext`.
Tạo EF migration:

```bash
dotnet ef migrations add AddInvitationLetters \
  --project src/Mms.Infrastructure \
  --startup-project src/Mms.Web
```

---

## BƯỚC 2 — BarQrCodeGenerator

### Packages cần thêm vào `Mms.Infrastructure.csproj`

- `QRCoder` (latest stable)
- `ZXing.Net` (latest stable)
- `DocumentFormat.OpenXml` 3.1.1

### Interface mới: `src/Mms.Application/Interfaces/IBarQrCodeGenerator.cs`

### File mới: `src/Mms.Infrastructure/Documents/bar-qr-code-generator.cs`

Tạo class `BarQrCodeGenerator` (implement `IBarQrCodeGenerator`):

- `byte[] GenerateBarcode(string content)` — Code128, 350×70px, PNG, margin 5px, dùng ZXing.Net `BarcodeWriterPixelData`
- `byte[] GenerateQrCode(string content)` — QR Error Correction M, 200×200px, PNG, dùng QRCoder `PngByteQRCode`
- `string BuildContent(string idNumber, string fullName)` — trả `$"{idNumber}|{fullName}"`

Register Singleton trong DI.

---

## BƯỚC 3 — LetterDocxBuilder

### File mới: `src/Mms.Infrastructure/Documents/letter-docx-builder.cs`

> **LAYOUT QUAN TRỌNG — A4 Gập 3 (C-Fold)**
>
> A4 dài 297mm, chia 3 phần ~99mm mỗi phần.
> Khi gập C-fold và nhét vào phong bì, **PHẦN ĐẦU TIÊN (Y=0–99mm từ đỉnh tờ giấy)** quay ra ngoài cửa sổ phong bì (4×12cm).
> **TOÀN BỘ thông tin cổ đông PHẢI nằm trong 99mm đầu tiên.**

```
┌──────────────────────────────┐  Y = 0mm
│  [Logo công ty]              │
│  CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
│  Độc lập – Tự do – Hạnh phúc│
│  Số: .../TB-HĐQT             │
│  THÔNG BÁO MỜI HỌP ĐHCĐ     │
│  ────────────────────────    │
│  Kính gửi: {{HoTen}}         │  ← hiện qua cửa sổ phong bì
│  Địa chỉ: {{DiaChi}}         │
│  Điện thoại: {{DienThoai}}   │
│  Số ĐKSH: {{SoDKSH}}         │
│  Số cổ phiếu: {{SoCoPhieu}}  │
│  [BARCODE_MARK]              │  ← OpenXML BookmarkStart/End
├──────────────────────────────┤  Y = 99mm  (ĐƯỜNG GẤP 1)
│  Kính thưa Quý Cổ đông, ...  │
│  1. Thời gian: ...           │
│  2. Địa điểm: ...            │
│  3. Nội dung: ...            │
│  4. Tài liệu: ...            │
│  5. Xác nhận tham dự: ...    │
├──────────────────────────────┤  Y = 198mm (ĐƯỜNG GẤP 2)
│  6. Ủy quyền: ...            │
│  7–9. ...                    │
│  Liên hệ: ...                │
│  [SEAL]       [SIGNATURE]    │  ← bookmarks
│  TM. HỘI ĐỒNG QUẢN TRỊ      │
└──────────────────────────────┘  Y = 297mm
```

**Logic `LetterDocxBuilder`:**

- Tạo synthetic template `.docx` in-memory bằng `DocumentFormat.OpenXml` nếu không có file template upload
- `BuildSingleLetterDocx(LetterDto dto, byte[]? codeMarkBytes) → byte[]`
  - Find-replace: traverse `Text` nodes tìm `{{placeholder}}` → replace inline
  - Insert barcode/QR PNG tại bookmark `BARCODE_MARK`: dùng `ImagePart` + `Drawing` + `Inline` element
  - Insert seal/signature PNG tại bookmarks `SEAL`, `SIGNATURE`
- `BuildMergedDocxAsync(IList<LetterDto> letters) → byte[]`
  - Dùng `AltChunk` với `AlternativeFormatImportPart` (contentType = DOCX)
  - Mỗi lá thư là 1 `AltChunk` → Word merge khi mở

**Interface `ILetterDocxBuilder`** đặt ở `src/Mms.Application/Interfaces/`.

---

## BƯỚC 4 — LibreOfficePdfConverter

### File mới: `src/Mms.Infrastructure/Documents/libre-office-pdf-converter.cs`

```csharp
public class LibreOfficePdfConverter : ILibreOfficePdfConverter
{
    public async Task<byte[]> ConvertDocxToPdfAsync(byte[] docxBytes, CancellationToken ct)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var tempDocx = Path.Combine(tempDir, "letter.docx");
        await File.WriteAllBytesAsync(tempDocx, docxBytes, ct);

        var psi = new ProcessStartInfo("libreoffice",
            $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{tempDocx}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync(ct);   // CancellationToken = timeout 60s

        var pdfPath = Path.ChangeExtension(tempDocx, ".pdf");
        return await File.ReadAllBytesAsync(pdfPath, ct);
    }
}
```

> LibreOffice đã có trong `docker-compose` — không cần cài thêm.

Interface `ILibreOfficePdfConverter` đặt ở `src/Mms.Application/Interfaces/`.
Register Transient trong DI.

---

## BƯỚC 5 — CpnRowMatcher

### File mới: `src/Mms.Infrastructure/Parsing/cpn-row-matcher.cs`

**5-tier matching algorithm (theo thứ tự ưu tiên):**

| Tier | Điều kiện | Confidence |
|---|---|---|
| 1. TrackingCode | CPN row có tracking code khớp exact với `InvitationLetter.TrackingCode` | High |
| 2. Name | `NormVN(FullName)` unique match (chỉ 1 cổ đông trong DB trùng) | High |
| 3. Phone | Phone last 9 digits unique match | High |
| 4. Name + Phone | `NormVN(Name)` + Phone last 9 combined | High |
| 5. Address prefix | Similarity ≥70% → flag manual review | Low |

**Helper `NormVN` — bắt buộc dùng chính xác logic này:**

```csharp
private static string NormVN(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return "";
    var d = s.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (char c in d)
        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            sb.Append(c);
    return Regex.Replace(sb.ToString().ToUpperInvariant(), @"\s+", " ").Trim();
}
```

**Output mỗi row:**

```csharp
record CpnMatchResult(
    Guid? InvitationLetterId,
    string CpnName,
    string? MatchedDbName,
    string? TrackingCode,
    MatchTier Tier,         // enum: TrackingCode, Name, Phone, NamePhone, Address, NoMatch
    MatchConfidence Confidence  // enum: High, Low, NoMatch
);
```

---

## BƯỚC 6 — Application Layer

### Thư mục `src/Mms.Application/InvitationLetters/`

**Commands:**

| Command | Logic |
|---|---|
| `GenerateLettersCommand` | Đọc `Shareholders` của Meeting → tạo `InvitationLetter` records (bỏ qua nếu đã tồn tại) → trả count tạo mới |
| `ExportLettersDocxCommand` | Nhận `MeetingId` + `CodeMarkType` → gọi `IBarQrCodeGenerator` + `ILetterDocxBuilder` → trả `(byte[] fileBytes, string fileName)` |
| `ExportLettersPdfCommand` | Như DOCX + gọi thêm `ILibreOfficePdfConverter` → trả `(byte[] fileBytes, string fileName)` |
| `ImportCpnReportCommand` | Nhận `MeetingId` + `Stream fileStream` + `CpnColumnMapping` + `bool DryRun` → parse Excel (ClosedXML) → `CpnRowMatcher` → nếu không DryRun: upsert TrackingCode + Status = Dispatched → trả `CpnImportResult` |
| `UpdateLetterStatusCommand` | Nhận `LetterId` + `InvitationStatus` + `string? FailureReason` → update + `StatusUpdatedAt = UtcNow` |

**Queries:**

| Query | Logic |
|---|---|
| `GetLettersQuery` | Paged, filter `Status?`, search Name/IdNumber, `AsNoTracking` |
| `GetLetterStatsQuery` | `COUNT GROUP BY Status` cho `MeetingId` |

**DTOs:**

```csharp
record LetterDto(string HoTen, string DiaChi, string DienThoai,
                 string SoDKSH, string SoCoPhieu,
                 CodeMarkType CodeMarkType, string? TrackingCode);

record CpnColumnMapping(string? NameColumn, string? PhoneColumn,
                        string? AddressColumn, string? TrackingCodeColumn);

record CpnImportResult(int Matched, int Unmatched, int LowConfidence,
                       IList<CpnMatchResult> LowConfidenceRows);
```

---

## BƯỚC 7 — Web UI Tab 1: Tạo & Xuất

### File mới: `src/Mms.Web/Components/Pages/Meetings/invitation-letters-page.razor`

**Route:** `@page "/meetings/{MeetingId:guid}/letters"`

**Tab 1 — "Tạo & Xuất":**

- `MudAlert` variant Info nếu chưa có letters
- Button **"🔄 Tạo danh sách thư"** → `GenerateLettersCommand` → toast success với count
- `MudSelect<CodeMarkType>` chọn Barcode / QRCode / None (default Barcode)
- Button **"📄 Xuất DOCX"** → `ExportLettersDocxCommand` → `NavigationManager.NavigateTo(downloadApiUrl)`
- Button **"📋 Xuất PDF"** → `ExportLettersPdfCommand` + `MudProgressLinear Indeterminate` khi processing
- Stats row: 4 `MudChip` — Tổng / Chưa gửi / Đã gửi / Trả lại (từ `GetLetterStatsQuery`)

**Download API endpoints** (Minimal API trong `src/Mms.Web/Endpoints/`):

```
GET /api/meetings/{id}/letters/export/docx
    → Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName)

GET /api/meetings/{id}/letters/export/pdf
    → Results.File(bytes, "application/pdf", fileName)
```

---

## BƯỚC 8 — Web UI Tab 2: Theo Dõi Giao Thư

**Tab 2 — "Theo dõi":**

- `MudDataGrid<LetterListItem>` server-side paging (PageSize=50), columns:
  - Tên CĐ / ĐKSH / SĐT / Tracking Code / Status / Ngày cập nhật / Actions
- Filter chips: All / NotSent / Dispatched / Delivered / Failed / Returned
- Status badge màu:

| Status | MudChip Color |
|---|---|
| NotSent | Default |
| Dispatched | Info |
| Delivered | Success |
| Failed | Error |
| Returned | Warning |

- Column Actions: icon button → `MudDialog` inline để sửa Status + FailureReason cho 1 record
- Gọi `UpdateLetterStatusCommand` khi confirm dialog

---

## BƯỚC 9 — Web UI Tab 3: Import Báo Cáo CPN

**Tab 3 — "Import CPN"** — `MudStepper` 3 bước:

**Bước 1 — Upload & Column Mapping:**

- `MudFileUpload` accept `.xlsx`
- Sau upload: đọc header row → `MudSelect<string>` cho từng trường (Họ tên / SĐT / Địa chỉ / Tracking code)
- Auto-restore mapping từ `ProtectedLocalStorage` key `"cpn_column_mapping_{MeetingId}"`
- Lưu mapping vào localStorage khi user thay đổi
- Button "Tiếp tục →"

**Bước 2 — Preview Kết Quả Match:**

- Gọi `ImportCpnReportCommand(DryRun=true)`
- Table: Tên CPN | Tên DB khớp | Tier | Confidence
- `Confidence=Low` → row highlight Warning
- `NoMatch` → row highlight Error
- Summary: `X matched / Y low confidence / Z no match`
- Buttons: "← Quay lại" | "✅ Xác nhận Import"

**Bước 3 — Kết Quả:**

- Gọi `ImportCpnReportCommand(DryRun=false)`
- `MudAlert Success`: "Đã cập nhật X / Y cổ đông"
- List rows `LowConfidence` để review thủ công
- Button "Xong" → active Tab 2

---

## Checklist Hoàn Thành

Sau khi implement xong tất cả 9 bước:

- [ ] `dotnet build` toàn solution → 0 errors
- [ ] `dotnet test tests/Mms.UnitTests/` → không có regression
- [ ] `docker-compose up -d` → navigate `/meetings/{id}/letters` → 3 tab render đúng
- [ ] Generate 5 letters → Xuất DOCX → mở file kiểm tra info block nằm trong 99mm đầu
- [ ] Xuất PDF → file mở được, layout đúng
- [ ] Import CPN file test → xem preview match → confirm → Tab 2 cập nhật status
- [ ] Update `docs/context_style_notes.md` thêm Phase 06A section (theo format các phase trước)

**Report cuối:** `DONE` hoặc `DONE_WITH_CONCERNS` theo format orchestration protocol.

> **Không viết unit test trong Phase 06A** — test coverage bổ sung ở phase sau.
