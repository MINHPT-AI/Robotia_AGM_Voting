# Phase 06A — Gửi Thư Mời Giấy (Physical Invitation Letter Management)

## Bối cảnh

Phase 06A là tính năng đầu tiên sau quality gate Phase 05. Mục đích: tạo, xuất, và theo dõi thư mời giấy gửi cho cổ đông tham dự ĐHCĐ. Thư mời được in theo mẫu DOCX có sẵn, in barcode/QR cá nhân hoá từng lá, xuất PDF hoặc DOCX gộp, sau đó cập nhật trạng thái giao thư từ file báo cáo CPN (bưu cục).

---

## Phạm vi Phase 06A

| Hạng mục | Mô tả |
|---|---|
| Entity | `InvitationLetter` + enum `InvitationStatus` + enum `CodeMarkType` |
| EF Migration | Bảng `invitation_letters` |
| Infrastructure Services | `BarQrCodeGenerator`, `LetterDocxBuilder`, `LibreOfficePdfConverter`, `CpnRowMatcher` |
| Application Layer | Commands/Queries: GenerateLetters, ExportLetters, GetLetters, ImportCpnReport, UpdateLetterStatus |
| Web UI | Route `/meetings/{id}/letters` — 3 tab page: Tạo+Xuất / Theo dõi / Import CPN |
| Template DOCX | A4 gập 3, info block nằm ở đoạn gấp ĐẦU TIÊN (0–99mm) |

---

## Packages Mới

| Package | Version | Mục đích |
|---|---|---|
| `QRCoder` | latest stable | QR code PNG (MIT) |
| `ZXing.Net` | latest stable | Code128 Barcode PNG (Apache 2.0) |
| `DocumentFormat.OpenXml` | 3.1.1 | Mở/sửa .docx template |
| (LibreOffice) | trong Docker | PDF conversion — đã có sẵn |
| (ClosedXML) | đã có | CPN Excel report parsing |

---

## Domain Entity

### `src/Mms.Domain/Entities/InvitationLetter.cs`

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

---

## Infrastructure Services

### 1. `BarQrCodeGenerator.cs`
- `GenerateBarcode(content)` → PNG 350×70px dùng ZXing.Net Code128
- `GenerateQrCode(content)` → PNG 200×200px dùng QRCoder
- `BuildContent(idNumber, fullName)` → `"IdNumber|FullName"` (format encode vào code mark)

### 2. `LetterDocxBuilder.cs`
- Nhận template .docx + list DTO → clone document theo template
- Find-replace placeholders: `{{HoTen}}`, `{{DiaChi}}`, `{{DienThoai}}`, `{{SoDKSH}}`, `{{SoCoPhieu}}`
- Insert barcode/QR PNG tại bookmark `BARCODE_MARK`
- Insert seal PNG tại bookmark `SEAL`, chữ ký tại `SIGNATURE`
- `BuildMergedDocxAsync()` → gộp nhiều lá thư vào 1 file DOCX dùng AltChunk
- **Tách riêng**: mỗi lá thư là 1 DOCX độc lập → merge sau

### 3. `LibreOfficePdfConverter.cs`
- Gọi `libreoffice --headless --convert-to pdf --outdir {tempDir} {tempDocx}` qua `Process.Start()`
- Timeout 60s per batch
- Trả về byte[] PDF

### 4. `CpnRowMatcher.cs`
Ghép cổ đông với TrackingCode từ file CPN bưu cục.

**Thuật toán khớp (priority order):**
1. TrackingCode (exact match nếu đã có trong DB)
2. FullName normalized (unique match)
3. Phone last 9 digits (unique match)
4. Name + Phone combined
5. Address prefix (≥70% similarity → low confidence, flag for manual review)

**Helper:**
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

---

## Template DOCX — Layout A4 Gập 3 (C-Fold)

> **QUAN TRỌNG:** A4 dài 297mm chia 3 phần, mỗi phần ~99mm.
> Thư được gập kiểu C-fold. Khi nhét vào phong bì, phần GẤP ĐẦU TIÊN (0–99mm từ đầu tờ giấy) sẽ quay ra ngoài cửa sổ phong bì (4×12cm).
> **Toàn bộ thông tin cổ đông + barcode PHẢI nằm trong 99mm đầu tiên.**

```
┌──────────────────────────┐  Y = 0mm
│  [Logo Công ty]          │
│  CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
│  Độc lập – Tự do – Hạnh phúc
│  ──────────────────────  │
│  Số: .../TB-HĐQT         │
│                          │
│  THÔNG BÁO MỜI HỌP       │
│  ĐẠI HỘI ĐỒNG CỔ ĐÔNG    │
│  ──────────────────────  │
│  Kính gửi: {{HoTen}}     │  ← hiện qua ô cửa sổ phong bì
│  Địa chỉ: {{DiaChi}}     │
│  Điện thoại: {{DienThoai}}│
│  Số ĐKSH: {{SoDKSH}}     │
│  Số cổ phiếu: {{SoCoPhieu}}│
│  [BARCODE_MARK]          │  ← barcode bookmark
├──────────────────────────┤  Y = 99mm  (ĐƯỜNG GẤP 1)
│  Kính thưa Quý Cổ đông,  │
│  Điểm 1: Thời gian...    │
│  Điểm 2: Địa điểm...     │
│  ...                     │
│  Điểm 5: ...             │
├──────────────────────────┤  Y = 198mm (ĐƯỜNG GẤP 2)
│  Điểm 6: ...             │
│  ...                     │
│  Điểm 9: ...             │
│  Liên hệ: ...            │
│  [SEAL] [SIGNATURE]      │
└──────────────────────────┘  Y = 297mm
```

**Ghi chú cho LetterDocxBuilder:**
- Set paragraph spacing / page margins sao cho content cổ đông (Kính gửi block) không vượt quá Y=95mm
- Khoảng trống dưới barcode nên pad đến Y=97mm để an toàn trước đường gấp
- Dùng Section break (không phải page break) để tách 3 vùng nếu cần kiểm soát chính xác vị trí

---

## Application Layer

### Commands
- `GenerateLettersCommand` — đọc shareholders từ DB → tạo records InvitationLetter
- `ExportLettersDocxCommand` — gộp DOCX + trả về file stream
- `ExportLettersPdfCommand` — convert PDF qua LibreOffice + trả về file stream
- `ImportCpnReportCommand` — parse CPN Excel → match → cập nhật TrackingCode + Status

### Queries
- `GetLettersQuery` — paged list với filter (status, search)
- `GetLetterStatsQuery` — count by status (cho dashboard tab)

---

## Web UI — `/meetings/{id}/letters`

### Tab 1: Tạo & Xuất
- Button "Tạo danh sách thư" → GenerateLettersCommand (nếu chưa có)
- Dropdown chọn CodeMarkType (Barcode / QR / None)
- Button "Xuất DOCX" → stream download
- Button "Xuất PDF" → stream download + progress indicator (async)
- Summary stats: tổng thư / chưa gửi / đã gửi / trả lại

### Tab 2: Theo dõi Giao Thư
- MudDataGrid: Tên / ĐKSH / SĐT / TrackingCode / Status / Ngày cập nhật
- Filter chip: All / NotSent / Dispatched / Delivered / Failed / Returned
- Inline edit Status + FailureReason
- Màu status badge: NotSent=default, Dispatched=info, Delivered=success, Failed=error, Returned=warning

### Tab 3: Import Báo Cáo CPN (3-step wizard)
- **Bước 1:** Upload CPN Excel, chọn mapping cột (tên / sdt / địa chỉ / tracking code)
  - Lưu column mapping vào localStorage
- **Bước 2:** Preview kết quả match (table: tên CPN | tên DB | status | confidence)
  - Highlight low-confidence rows (address-only match)
- **Bước 3:** Confirm → ImportCpnReportCommand → show results summary

---

## Execution Order (9 Bước)

| Bước | Nội dung | Est. Time |
|------|----------|-----------|
| 1 | Domain Entity + Enum + EF Migration | 0.5h |
| 2 | BarQrCodeGenerator service | 0.5h |
| 3 | LetterDocxBuilder service (template + placeholder + bookmark) | 2h |
| 4 | LibreOfficePdfConverter service | 0.5h |
| 5 | CpnRowMatcher service (NormVN + 5-tier matching) | 1h |
| 6 | Application Layer: Commands + Queries + Validators | 2h |
| 7 | Web UI: Tab 1 — Tạo & Xuất | 1.5h |
| 8 | Web UI: Tab 2 — Delivery Tracking Grid | 1.5h |
| 9 | Web UI: Tab 3 — Import CPN Wizard | 2h |

**Tổng ước tính: ~11.5h** (1.5–2 ngày làm việc)

---

## Open Questions (Đã giải quyết)

| # | Câu hỏi | Quyết định |
|---|---------|-----------|
| Q1 | CPN file format? | Excel (.xlsx), cột linh hoạt → column mapping UI, lưu localStorage |
| Q2 | Số lượng cổ đông? | Tối đa ~10,000 → DOCX instant, PDF async + progress bar |
| Q3 | Template DOCX có sẵn chưa? | AI tự tạo synthetic template khi implement |
| Q4 | Barcode hay QR? | Cả 2, user chọn per-meeting, mặc định Barcode |
| Q5 | Thông tin bưu cục in trên thư không? | Không — chỉ thông tin cổ đông + code mark |

---

## Verification Checklist

- [ ] EF Migration chạy clean, bảng `invitation_letters` tạo đúng
- [ ] Barcode + QR generate ra PNG đúng kích thước
- [ ] DOCX gộp 10 letters mở được trong Word/LibreOffice
- [ ] PDF convert thành công qua LibreOffice headless
- [ ] Info block cổ đông nằm trong 99mm đầu của tờ A4
- [ ] CPN match đúng: TrackingCode → name → phone → name+phone → address
- [ ] UI 3 tab render đúng, filter hoạt động
- [ ] Export DOCX/PDF download đúng content-type
- [ ] `docker-compose up` → full flow chạy không lỗi

---

## Notes cho AI thực thi

1. **LibreOffice đã có trong docker-compose** — không cần cài thêm, chỉ cần `Process.Start("libreoffice", ...)`
2. **ClosedXML đã có** trong Infrastructure project — dùng cho CPN Excel parsing
3. **Template .docx**: AI tự tạo synthetic template với placeholder text và 3 bookmarks (`BARCODE_MARK`, `SEAL`, `SIGNATURE`) dùng `DocumentFormat.OpenXml`; không cần file template tĩnh
4. **OpenXML bookmarks**: dùng `BookmarkStart`/`BookmarkEnd` + `Run` với `Drawing` element để insert image
5. **Không dùng WebApplicationFactory** cho bất kỳ test nào liên quan Blazor Server
6. **Column mapping UI** cho CPN: dùng `MudSelect<string>` bind vào Dictionary, persist qua `ProtectedLocalStorage`
