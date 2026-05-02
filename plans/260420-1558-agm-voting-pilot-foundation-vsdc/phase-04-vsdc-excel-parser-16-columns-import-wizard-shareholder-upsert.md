# Phase 04 — VSDC Excel Parser (16 Fixed Columns) + Import Wizard + Shareholder Upsert

## Context Links

- Parent plan: [`./plan.md`](./plan.md)
- Dependency: [`./phase-03-company-info-meeting-crud-resolutions-candidates.md`](./phase-03-company-info-meeting-crud-resolutions-candidates.md) (meeting_id + company.total_voting_shares cần có)
- Dependency: [`./phase-01-database-auth-identity.md`](./phase-01-database-auth-identity.md) (bảng `shareholders` + UNIQUE index)
- Brainstorm: [`../reports/brainstorm-260420-1558-agm-voting-system-architecture.md`](../reports/brainstorm-260420-1558-agm-voting-system-architecture.md) § 3.3 (DB Schema shareholders), § 4 (NFR performance)
- BRD: [`../../brd-quy-trinh-dhcd.md`](../../brd-quy-trinh-dhcd.md) — **Bước 2** Import Danh sách Cổ đông (đọc kỹ toàn bộ section này)
- UI Spec: [`../../ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md`](../../ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md) — **§C2** Import VSDC Wizard 4 bước
- VSDC sample: [`../../ExempleTemplate_file/Mẫu file DS VSDC gui.xlsx`](../../ExempleTemplate_file/Mẫu%20file%20DS%20VSDC%20gui.xlsx)
- VSDC sample CSV: [`../../ExempleTemplate_file/Mẫu file DS VSDC gui.csv`](../../ExempleTemplate_file/Mẫu%20file%20DS%20VSDC%20gui.csv)

---

## Overview

- **Tuần**: 5
- **Priority**: P1 (critical path — đây là tính năng cốt lõi nhất của pilot)
- **Status**: pending
- **Brief**: Phase phức tạp nhất pilot. Implement VsdcParser đọc file Excel thô từ VSDC (merged cells, multi-row headers), ánh xạ cứng 16 cột cố định, validate dữ liệu, sau đó upsert hàng loạt vào DB với target < 10s cho 1,000 cổ đông. UI: Import Wizard 4 bước (MudStepper).

---

## Key Insights

- **File VSDC là báo cáo thô**: KHÔNG phải spreadsheet data sạch. Có nhiều dòng header lồng nhau, ô gộp (merged cells) đặc biệt ở cột 11-16 (các sub-column "Chưa lưu ký" / "Lưu ký" / "Tổng cộng"). Parser phải locate đúng dòng data bắt đầu (skip header rows).
- **16 cột CỐ ĐỊNH — không config linh hoạt**: BRD yêu cầu rõ "đọc cố định đúng trật tự 16 cột tiêu chuẩn". Parser KHÔNG cho phép user remap. Sai format → báo lỗi rõ ràng.
- **3 cột tối quan trọng**:
  - **Cột 5 (Số ĐKSH)** = `id_number` = primary key của cổ đông trong meeting (UNIQUE).
  - **Cột 10 (Quốc tịch)** = `nationality` → sau này dùng detect template ngôn ngữ (VN/EN).
  - **Cột 16 (SL Quyền phân bổ Tổng cộng)** = `voting_rights` = số phiếu biểu quyết thực tế. **MANDATORY > 0**.
- **Upsert strategy**: `INSERT ... ON CONFLICT (meeting_id, id_number) DO UPDATE` — Postgres native upsert. Nhanh hơn EF check-then-insert.
- **Performance < 10s cho 1,000 rows**: dùng `EF Core Bulk` (`AddRange` + single `SaveChanges`) hoặc raw SQL `COPY`. EF `AddRange` với 1,000 records + disabled change tracker đủ đạt target. Không cần Npgsql COPY trừ khi bench fail.
- **Wizard state lưu trong memory (Blazor Server)**: không cần session/localStorage — Blazor Server giữ state trong circuit. File upload xử lý xong → parse result lưu vào `@code` variable → truyền giữa steps.
- **Validation**: chạy toàn bộ trên parsed data TRƯỚC khi insert. Không insert partial — hoặc toàn bộ thành công hoặc không insert gì (transaction).

---

## Requirements

### Functional

- [F-04.1] Step 1/4 Upload: nhận file `.xlsx`/`.xls` max 10MB. Hiển thị lần import trước (nếu có).
- [F-04.2] Step 2/4 Map: hiển thị bảng preview 16 cột đã map (read-only, không cho user thay đổi). Parse file xác định đúng header row và hiển thị 5 dòng đầu preview.
- [F-04.3] Step 3/4 Validate: hiển thị tổng hợp lỗi + cảnh báo. Cho filter "chỉ hiện lỗi" / "chỉ hiện cảnh báo". Hiển thị tổng CP import vs VĐL (%).
- [F-04.4] Step 4/4 Execute: import thành công → hiển thị report (tổng / thành công / bỏ qua / mới / cập nhật).
- [F-04.5] Validation rules:
  - ❌ `MISSING_ID_NUMBER`: Cột 5 rỗng hoặc null.
  - ❌ `MISSING_NAME`: Cột 2 rỗng.
  - ❌ `ZERO_VOTING_RIGHTS`: Cột 16 ≤ 0 hoặc null.
  - ⚠️ `DUPLICATE_ID_NUMBER`: `(meeting_id, id_number)` đã tồn tại → offer update.
  - ⚠️ `EXCEEDS_CHARTER_CAPITAL`: SUM(voting_rights) > company.total_voting_shares.
- [F-04.6] Upsert: INSERT nếu chưa có `(meeting_id, id_number)`; UPDATE nếu đã có.
- [F-04.7] Toàn bộ import trong 1 transaction: thành công toàn bộ hoặc rollback toàn bộ.
- [F-04.8] Ghi audit log sau import thành công: `category=Import`, detail chứa count.

### Non-Functional

- [NF-04.1] **Parse 1,000 rows < 3s** (CPU bound — parsing Excel).
- [NF-04.2] **DB upsert 1,000 rows < 7s** (tổng < 10s end-to-end).
- [NF-04.3] UI không freeze khi parse: parse chạy trong `Task.Run()` với progress indicator.
- [NF-04.4] File upload: stream trực tiếp, không load toàn bộ vào memory nếu file > 5MB.
- [NF-04.5] Import idempotent: import cùng file 2 lần → kết quả giống nhau (upsert, không duplicate).

---

## Architecture

### VSDC File Structure (từ sample)

```
Dòng 1-N:  Header lồng nhau (merge cells, tên công ty, ngày chốt, ...)
Dòng X:    Header chính: "STT | Họ và tên | SID | Mã NĐT | Số ĐKSH | ..."
Dòng X+1:  Sub-header cho cột 11-16 (merged: "Chưa lưu ký | Lưu ký | Tổng cộng")
Dòng X+2:  Bắt đầu data rows
```

Parser phải locate `headerRowIndex` bằng cách scan tìm cell chứa chuỗi "STT" hoặc "Họ và tên" (case-insensitive). Data bắt đầu từ `headerRowIndex + 2` (skip 1 sub-header row).

### 16 Cột Mapping (CỐ ĐỊNH)

| Index (0-based) | Tên cột VSDC | Field DB | Bắt buộc |
|-----------------|--------------|----------|----------|
| 0 | STT | `vsdc_row` | ❌ |
| 1 | Họ và tên | `full_name` | ✅ |
| 2 | Mã định danh NĐT (SID) | `sid` | ❌ |
| 3 | Mã nhà đầu tư | `investor_code` | ❌ |
| 4 | **Số ĐKSH** | **`id_number`** | ✅ PRIMARY KEY |
| 5 | Ngày cấp | `id_issue_date` | ❌ |
| 6 | Địa chỉ | `address` | ❌ |
| 7 | Email | `email` | ❌ |
| 8 | Điện thoại | `phone` | ❌ |
| 9 | **Quốc tịch** | **`nationality`** | ✅ multilang |
| 10 | SL CP nắm giữ (Chưa lưu ký) | `shares_non_deposit` | ❌ |
| 11 | SL CP nắm giữ (Lưu ký) | `shares_deposit` | ❌ |
| 12 | SL CP nắm giữ (Tổng cộng) | `shares_total` | ❌ |
| 13 | SL Quyền phân bổ (Chưa lưu ký) | `rights_non_deposit` | ❌ |
| 14 | SL Quyền phân bổ (Lưu ký) | `rights_deposit` | ❌ |
| 15 | **SL Quyền phân bổ (Tổng cộng)** | **`voting_rights`** | ✅ MANDATORY |

> **Lưu ý index**: file VSDC dùng 1-based column (Cột 5 trong BRD = index 4 trong code).

### Parser Pipeline

```
ExcelFileStream
    │
    ▼
ClosedXML.Excel.XLWorkbook.Load(stream)
    │
    ▼
HeaderLocator.FindHeaderRow(worksheet)
  → scan rows, find cell matching "STT" or "Họ và tên"
  → return headerRowIndex
    │
    ▼
DataRowExtractor.ExtractRows(worksheet, startRow = headerRowIndex + 2)
  → iterate rows đến khi gặp empty row hoặc EOF
  → mỗi row: đọc 16 cells theo index cố định
  → return List<VsdcRawRow>
    │
    ▼
VsdcRowMapper.Map(rawRows) → List<ShareholderImportDto>
  → parse types: long cho shares, DateTime cho date, string trim cho text
  → null-safe: empty cell → null/0
    │
    ▼
VsdcValidator.Validate(dtos, existingIdNumbers, totalVotingSharesVDL)
  → return VsdcValidationResult { Valid[], Errors[], Warnings[] }
```

### Import Command Flow

```
ImportWizardPage (Blazor)
    │ Step 4 user clicks "Import"
    │
    ▼
IMediator.Send(ImportShareholdersCommand { MeetingId, ValidRows })
    │
    ▼
ImportShareholdersHandler
    ├── BEGIN TRANSACTION
    ├── Build List<Shareholder> from ValidRows
    ├── context.ChangeTracker.AutoDetectChangesEnabled = false  (perf)
    ├── Raw SQL UPSERT:
    │   INSERT INTO shareholders (...) VALUES (...)
    │   ON CONFLICT (meeting_id, id_number)
    │   DO UPDATE SET full_name=EXCLUDED.full_name, voting_rights=EXCLUDED.voting_rights, ...
    ├── COMMIT
    ├── IAuditLogService.LogAsync(Import, ...)
    └── return ImportResult { TotalRead, Inserted, Updated, Skipped }
```

---

## Related Code Files

### Tạo mới

```
src/Mms.Infrastructure/
├── Parsing/
│   ├── VsdcParser.cs                         # ⭐ core parser — locate header + extract rows
│   ├── VsdcRowMapper.cs                       # map raw cells → ShareholderImportDto
│   ├── VsdcValidator.cs                       # validation rules → VsdcValidationResult
│   ├── VsdcRawRow.cs                          # intermediate: 16 raw string cells
│   └── VsdcValidationResult.cs               # Valid[], Errors[], Warnings[], summary stats

src/Mms.Application/
├── Shareholders/
│   ├── Commands/ImportShareholdersCommand.cs  # { MeetingId, List<ShareholderImportDto> }
│   ├── Dtos/ShareholderImportDto.cs           # mapped từ VsdcRawRow
│   └── Dtos/ImportResultDto.cs               # { TotalRead, Inserted, Updated, Skipped, Errors }

src/Mms.Infrastructure/
└── Handlers/Shareholders/
    └── ImportShareholdersHandler.cs           # EF bulk upsert + audit

src/Mms.Web/Pages/Meetings/
├── ImportWizardPage.razor                     # @page "/meetings/{Id:guid}/import"
└── Components/
    ├── ImportStep1Upload.razor                # upload file UI
    ├── ImportStep2Preview.razor               # 16-column mapping preview
    ├── ImportStep3Validate.razor              # validation result grid
    └── ImportStep4Result.razor                # import report

src/Mms.Web/Api/
└── UploadsController.cs                       # POST /api/meetings/{id}/import/upload
                                               # stream file → VsdcParser → return parse result
```

### Sửa

```
src/Mms.Infrastructure/Persistence/Migrations/
└── (migration mới nếu cần alter shareholders table)

src/Mms.Web/Shared/NavMenu.razor               # thêm link "Import VSDC" vào meeting submenu
```

---

## Implementation Steps

### Bước 1: Cài ClosedXML

1. Cài NuGet `ClosedXML` (MIT license) vào `Mms.Infrastructure`.
2. Verify license: ClosedXML là MIT — OK cho commercial use.
3. KHÔNG dùng EPPlus v5+ (commercial license GPLv3/commercial).

### Bước 2: VsdcRawRow + VsdcParser

1. `VsdcRawRow.cs`:
   ```csharp
   public record VsdcRawRow(int RowIndex, string?[] Cells); // Cells.Length == 16
   ```

2. `VsdcParser.cs`:
   ```csharp
   public VsdcParseResult Parse(Stream excelStream)
   {
       using var workbook = new XLWorkbook(excelStream);
       var ws = workbook.Worksheet(1); // first sheet

       // 1. Locate header row
       int headerRow = FindHeaderRow(ws);
       if (headerRow < 0) throw new VsdcFormatException("Không tìm thấy header VSDC");

       // 2. Extract data rows (skip 1 sub-header row after header)
       int dataStartRow = headerRow + 2;
       var rawRows = new List<VsdcRawRow>();
       int row = dataStartRow;
       while (!ws.Row(row).IsEmpty())
       {
           var cells = Enumerable.Range(1, 16)
               .Select(col => ws.Cell(row, col).GetString()?.Trim())
               .ToArray();
           rawRows.Add(new VsdcRawRow(row, cells));
           row++;
       }
       return new VsdcParseResult(rawRows, headerRow, dataStartRow);
   }

   private int FindHeaderRow(IXLWorksheet ws)
   {
       // Scan first 30 rows, look for cell containing "STT" or "Họ và tên"
       for (int r = 1; r <= 30; r++)
       {
           var firstCell = ws.Cell(r, 1).GetString();
           var secondCell = ws.Cell(r, 2).GetString();
           if (firstCell?.Contains("STT", StringComparison.OrdinalIgnoreCase) == true
               || secondCell?.Contains("Họ và tên", StringComparison.OrdinalIgnoreCase) == true)
               return r;
       }
       return -1;
   }
   ```

### Bước 3: VsdcRowMapper

```csharp
public static ShareholderImportDto Map(VsdcRawRow raw)
{
    return new ShareholderImportDto
    {
        VsdcRow        = TryParseInt(raw.Cells[0]),
        FullName       = raw.Cells[1],
        Sid            = raw.Cells[2],
        InvestorCode   = raw.Cells[3],
        IdNumber       = raw.Cells[4],           // Cột 5 (index 4)
        IdIssueDate    = TryParseDate(raw.Cells[5]),
        Address        = raw.Cells[6],
        Email          = raw.Cells[7],
        Phone          = raw.Cells[8],
        Nationality    = raw.Cells[9],            // Cột 10 (index 9)
        SharesNonDeposit  = TryParseLong(raw.Cells[10]),
        SharesDeposit     = TryParseLong(raw.Cells[11]),
        SharesTotal       = TryParseLong(raw.Cells[12]),
        RightsNonDeposit  = TryParseLong(raw.Cells[13]),
        RightsDeposit     = TryParseLong(raw.Cells[14]),
        VotingRights      = TryParseLong(raw.Cells[15]),  // Cột 16 (index 15)
    };
}
// Helpers: TryParseInt, TryParseLong, TryParseDate (locale-aware dd/MM/yyyy)
```

### Bước 4: VsdcValidator

```csharp
public VsdcValidationResult Validate(
    List<ShareholderImportDto> rows,
    HashSet<string> existingIdNumbers,   // từ DB query trước
    long totalVotingSharesVdl)
{
    var errors = new List<VsdcRowError>();
    var warnings = new List<VsdcRowWarning>();

    foreach (var row in rows)
    {
        if (string.IsNullOrWhiteSpace(row.IdNumber))
            errors.Add(new(row.VsdcRow, "MISSING_ID_NUMBER", "Thiếu Số ĐKSH"));

        if (string.IsNullOrWhiteSpace(row.FullName))
            errors.Add(new(row.VsdcRow, "MISSING_NAME", "Thiếu Họ tên"));

        if (row.VotingRights <= 0)
            errors.Add(new(row.VsdcRow, "ZERO_VOTING_RIGHTS", "Số CP biểu quyết = 0"));

        if (!string.IsNullOrWhiteSpace(row.IdNumber)
            && existingIdNumbers.Contains(row.IdNumber))
            warnings.Add(new(row.VsdcRow, "DUPLICATE_ID_NUMBER",
                             $"ĐKSH {row.IdNumber} đã tồn tại — sẽ cập nhật"));
    }

    long totalRights = rows.Where(r => r.VotingRights > 0).Sum(r => r.VotingRights);
    if (totalRights > totalVotingSharesVdl)
        warnings.Add(new(-1, "EXCEEDS_CHARTER_CAPITAL",
                         $"Tổng CP {totalRights:N0} vượt VĐL {totalVotingSharesVdl:N0}"));

    return new VsdcValidationResult(rows, errors, warnings, totalRights, totalVotingSharesVdl);
}
```

### Bước 5: ImportShareholdersHandler (Bulk Upsert)

```csharp
public async Task<ImportResultDto> Handle(ImportShareholdersCommand cmd, CancellationToken ct)
{
    var shareholders = cmd.ValidRows.Select(dto => new Shareholder {
        Id = Guid.NewGuid(),
        MeetingId = cmd.MeetingId,
        // ... map all fields
    }).ToList();

    // Performance: disable change tracker
    _context.ChangeTracker.AutoDetectChangesEnabled = false;

    await using var tx = await _context.Database.BeginTransactionAsync(ct);
    try
    {
        // Postgres native UPSERT via raw SQL for bulk performance
        // hoặc dùng EF AddRange nếu size <= 2000 rows
        int inserted = 0, updated = 0;

        // Chunk thành batches 500 để tránh parameter limit
        foreach (var batch in shareholders.Chunk(500))
        {
            // Check existing trong batch
            var ids = batch.Select(s => s.IdNumber).ToHashSet();
            var existing = await _context.Shareholders
                .Where(s => s.MeetingId == cmd.MeetingId && ids.Contains(s.IdNumber))
                .ToDictionaryAsync(s => s.IdNumber, ct);

            var toInsert = new List<Shareholder>();
            var toUpdate = new List<Shareholder>();

            foreach (var s in batch)
            {
                if (existing.TryGetValue(s.IdNumber, out var ex))
                {
                    // Update existing
                    ex.FullName = s.FullName;
                    ex.VotingRights = s.VotingRights;
                    // ... other fields
                    toUpdate.Add(ex);
                    updated++;
                }
                else
                {
                    toInsert.Add(s);
                    inserted++;
                }
            }

            if (toInsert.Any()) _context.Shareholders.AddRange(toInsert);
            await _context.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        await _auditLog.LogAsync("Import", "Shareholder", cmd.MeetingId.ToString(),
            new { inserted, updated, total = shareholders.Count }, cmd.UserId);

        return new ImportResultDto(shareholders.Count, inserted, updated, 0);
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
    finally
    {
        _context.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
```

> **Nếu bench thất bại (> 10s)**: upgrade sang `Npgsql COPY` binary mode hoặc `EFCore.BulkExtensions` (MIT).

### Bước 6: Upload API Endpoint

1. `UploadsController.cs` — `POST /api/meetings/{meetingId}/import/upload`:
   - Nhận `IFormFile` (max 10MB check bằng `RequestSizeLimit`).
   - Stream vào `VsdcParser.Parse(stream)`.
   - Gọi `VsdcRowMapper.Map()` cho tất cả rows.
   - Trả `ParsedImportSessionDto { Rows, ParsedAt, TotalRows }`.
   - **Không** lưu file vào disk — parse trực tiếp từ stream rồi trả JSON.

2. Blazor page gọi upload qua `HttpClient` inject — KHÔNG dùng `IBrowserFile.OpenReadStream()` trực tiếp (giới hạn 512KB default).

### Bước 7: Import Wizard UI (MudStepper)

```razor
@page "/meetings/{MeetingId:guid}/import"
[Authorize(Roles = "admin,operator")]

<MudStepper @bind-ActiveIndex="@_activeStep" Linear="true">
    <MudStep Title="Upload File">
        <ImportStep1Upload OnFileUploaded="HandleFileUploaded" />
    </MudStep>
    <MudStep Title="Xem cột dữ liệu">
        <ImportStep2Preview ParseResult="@_parseResult" />
    </MudStep>
    <MudStep Title="Kiểm tra dữ liệu">
        <ImportStep3Validate ValidationResult="@_validationResult"
                             OnProceed="HandleValidationProceed" />
    </MudStep>
    <MudStep Title="Kết quả">
        <ImportStep4Result ImportResult="@_importResult" />
    </MudStep>
</MudStepper>
```

**Step 1 — Upload**:
- Giao diện kéo thả file. Khác với thiết kế tĩnh (có Dropdown chọn Meeting), trang này sẽ lấy Meeting trực tiếp từ URL `/meetings/{MeetingId:guid}/import` và hiển thị Tên Cuộc Họp + Ngày ĐKCC dưới dạng Read-only.
- Bảng lịch sử Import hiển thị danh sách các lần import trước đó của cuộc họp.

**Step 2 — Preview (Read-only)**:
- Giao diện sẽ giống dạng form Select nhưng sẽ bị khóa (Disabled/Read-only) nhằm đảm bảo tiêu chí **16 cột map cố định** không cho phép User chỉnh sửa tránh sai lệch cấu trúc VSDC.

**Step 3 — Validate Grid**:
- `MudDataGrid` hoặc HTML Table với `ServerData` hoặc local data.
- Các Widget đếm: Hợp lệ, Lỗi, Cảnh báo.
- Checkbox filter: "Chỉ hiện lỗi", "Chỉ hiện cảnh báo", thanh Search.
- Summary bar: thanh Progress % (Tổng CP / VĐL).
- Nút "Import N dòng" chỉ cho phép khi không có lỗi nghiêm trọng (đỏ).

**Step 4 — Result**:
- Thẻ Widget báo cáo: Tổng dòng đọc, Import thành công, Tổng CP, Bỏ qua (lỗi).
- Widget thống kê nhân khẩu học: Phân loại "Cá nhân" vs "Tổ chức" (dựa vào bóc tách chuỗi `InvestorCode`), "Trong nước" vs "Nước ngoài" (dựa vào cột `Nationality` = 'VN' hay khác).
- Nút [Xem danh sách cổ đông] → navigate tới danh sách.

---

## Todo List

- [ ] Cài ClosedXML NuGet vào Mms.Infrastructure (verify MIT license)
- [ ] Tạo VsdcRawRow record
- [ ] Tạo VsdcParser (FindHeaderRow + ExtractRows)
- [ ] Test VsdcParser với file sample `Mẫu file DS VSDC gui.xlsx` thủ công
- [ ] Tạo VsdcRowMapper (16 fields mapping + helper parsers)
- [ ] Tạo VsdcValidationResult + VsdcRowError + VsdcRowWarning
- [ ] Tạo VsdcValidator (5 validation rules)
- [ ] Tạo ShareholderImportDto + ImportResultDto
- [ ] Tạo ImportShareholdersCommand + Handler (bulk upsert, batch 500)
- [ ] Benchmark import 1,000 rows → verify < 10s
- [ ] Tạo UploadsController POST /api/meetings/{id}/import/upload
- [ ] Tạo ImportWizardPage.razor (MudStepper 4 steps)
- [ ] Tạo ImportStep1Upload.razor (MudFileUpload)
- [ ] Tạo ImportStep2Preview.razor (16-col mapping table read-only)
- [ ] Tạo ImportStep3Validate.razor (grid + toggle filter + summary bar)
- [ ] Tạo ImportStep4Result.razor (result cards)
- [ ] Thêm link "Import VSDC" vào NavMenu và MeetingListPage
- [ ] Unit test VsdcParser (6 cases — xem phase-05)
- [ ] Integration test import end-to-end (Testcontainers — xem phase-05)

---

## Success Criteria

- [ ] Parser đọc đúng `Mẫu file DS VSDC gui.xlsx` — số rows bằng số CĐ trong file.
- [ ] Cột 5 (id_number), Cột 10 (nationality), Cột 16 (voting_rights) đọc đúng giá trị.
- [ ] Validation phát hiện đúng: 1 row thiếu CMND + 1 row CP=0 + 1 row trùng CMND.
- [ ] Import 1,000 rows lần 1 → inserted=1000, updated=0. Lần 2 cùng file → inserted=0, updated=1000.
- [ ] Import 1,000 rows hoàn thành < 10s (đo bằng test Stopwatch).
- [ ] Import fail giữa chừng (mock DB error) → rollback toàn bộ (bảng trống).
- [ ] Audit log có 1 row sau import thành công.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **VSDC thay đổi format** (thêm/bớt cột, đổi tên header) | High | Parser validate: nếu `FindHeaderRow` fail → throw `VsdcFormatException` rõ ràng với hướng dẫn liên hệ support. Log file bytes (first 1KB) cho debug. |
| **Merged cells làm ClosedXML đọc sai index** | High | Test kỹ với sample file thực. Merged cells ở header rows → chỉ ảnh hưởng header detection, không ảnh hưởng data rows (data rows không merge). |
| **Import > 10s (performance fail)** | Medium | Fallback plan: raw SQL `INSERT ... ON CONFLICT DO UPDATE` với Npgsql batch. Đo benchmark ngay bước 1 trước khi làm UI. |
| **File upload > memory** | Medium | Stream trực tiếp (không `ReadAllBytes`). Cấu hình `MaxRequestBodySize = 10MB`. |
| **Encoding tiếng Việt trong Excel** | Low | ClosedXML đọc UTF-8 / Windows-1252 tự động. Test với file có dấu tiếng Việt. |
| **Duplicate id_number trong cùng 1 file upload** | Medium | Validate trong `VsdcValidator`: group by `id_number`, nếu count > 1 → error `INTRA_FILE_DUPLICATE`. |

---

## Security Considerations

- File upload: validate MIME type (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` hoặc `application/vnd.ms-excel`). Không chấp nhận file ngoài danh sách.
- Giới hạn `RequestSizeLimit(10 * 1024 * 1024)` trên endpoint upload.
- Không lưu file upload vào disk (parse-and-discard) → không rủi ro path traversal hay disk exhaustion.
- Import chỉ dành cho `admin` và `operator` — `[Authorize(Roles = "admin,operator")]`.
- Injection: toàn bộ data đi qua EF parameterized — không raw string concat.

---

## Next Steps

- Phase-05: unit test `VsdcParser` với ít nhất 6 cases bao gồm edge cases (file lỗi, dòng trống xen kẽ, merged cells).
- Phase-05: integration test bulk upsert với Testcontainers Postgres, đo thời gian.
- Phase-05: Playwright E2E scenario "Upload VSDC → verify count".
- Phase sau (POS Check-in): bảng `shareholders` đã có đủ data → chỉ cần join với `ballots`.

---

## Unresolved Questions

1. **File VSDC thực tế từ khách hàng pilot**: format có khớp với `Mẫu file DS VSDC gui.xlsx` không? Cần nhận 1 file production (ẩn danh hóa) để test parser trước khi demo.
2. **Sub-header row count**: file sample có đúng 1 sub-header row giữa header chính và data không? Hay có file có 0 hoặc 2 sub-header rows?
3. **Encoding CSV**: file `.csv` có cùng structure với `.xlsx` không? Có cần hỗ trợ CSV không (BRD chỉ nhắc `.xlsx`/`.xls`)?
4. **Bulk upsert library**: nếu EF batch không đủ tốc độ < 10s, dùng `EFCore.BulkExtensions` (MIT) hay raw Npgsql COPY? Cần benchmark sớm.
