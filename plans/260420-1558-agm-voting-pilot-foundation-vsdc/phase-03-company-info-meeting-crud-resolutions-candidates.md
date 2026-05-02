# Phase 03 — Company Info + Meeting CRUD (Resolutions & Candidates Grid)

## Context Links

- Parent plan: [`./plan.md`](./plan.md)
- Dependency: [`./phase-02-blazor-server-mudblazor-login-dashboard-ui.md`](./phase-02-blazor-server-mudblazor-login-dashboard-ui.md) (shell + auth phải xong)
- Dependency: [`./phase-01-database-auth-identity.md`](./phase-01-database-auth-identity.md) (DB schema + EF migrations)
- Brainstorm: [`../reports/brainstorm-260420-1558-agm-voting-system-architecture.md`](../reports/brainstorm-260420-1558-agm-voting-system-architecture.md) § 3.3 (DB Schema), § 3.4 (Audit)
- BRD: [`../../brd-quy-trinh-dhcd.md`](../../brd-quy-trinh-dhcd.md) — Bước 1 §Thông tin DN, §Thiết lập Cuộc họp, §Thiết lập Đại hội
- UI Spec sections:
  - B2 Thông tin DN: `ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md` §B2
  - C1 DS + Form Cuộc họp: §C1

---

## Overview

- **Tuần**: 4
- **Priority**: P1 (Phase-04 cần `meeting_id` để import shareholder)
- **Status**: pending
- **Brief**: Implement 2 nhóm tính năng CRUD: (1) Thông tin Doanh nghiệp (B2) — cấu hình 1 lần, lưu global; (2) Quản lý Cuộc họp (C1) — tạo/sửa/xóa meeting với 2 sub-grid inline: DS Nội dung biểu quyết (Tờ trình) và DS Ứng viên bầu cử. CQRS via MediatR. Audit log mọi thao tác create/update/delete.

---

## Key Insights

- **Company là singleton per deployment**: trong pilot, giả định 1 công ty / 1 instance hệ thống. `GET /companies` → trả record đầu tiên (nếu chưa có → redirect setup wizard đơn giản).
- **Meeting lifecycle chỉ lưu trạng thái**: pilot chưa cần enforce state machine transition logic — chỉ cần `status` field readable. Transition logic implement ở phase check-in / kiểm phiếu.
- **Sub-grid inline**: Resolutions + Candidates là child entities của Meeting. Dùng `MudDataGrid` inline editing với Add/Edit/Delete row — không mở dialog riêng để nhanh nhất.
- **Nhân bản meeting**: clone meeting + resolutions + candidates nhưng KHÔNG clone shareholders/ballots — chỉ copy metadata. Hữu ích cho ĐHCĐ năm sau.
- **CQRS MediatR**: Application layer dùng `IRequest`/`IRequestHandler`. Infrastructure implement handlers. Web layer chỉ inject `IMediator.Send()`.
- **FluentValidation**: validate commands ở Application layer, không ở Web layer. Tránh duplicate validation.
- **Tổng CP tự fill**: Form tạo meeting → field "Tổng CP có quyền biểu quyết" tự fill từ `company.total_voting_shares`, nhưng cho phép override (vì ngày chốt có thể khác).

---

## Requirements

### Functional

**B2 — Thông tin Doanh nghiệp**
- [F-03.1] Admin xem + sửa 21 fields thông tin công ty (bổ sung: Website, Tên tiếng Anh, Mã chứng khoán, Sàn niêm yết, Con dấu, Chữ ký). Cần tạo EF Migration để thêm các trường này vào DB.
- [F-03.2] Upload Logo, Con dấu, Chữ ký đại diện (PNG/SVG/JPG, max 2MB) → lưu `wwwroot/uploads/` tương ứng → hiển thị preview.
- [F-03.3] Validate: MST 10 hoặc 13 ký tự số; Vốn điều lệ > 0; Tổng CP BQ ≤ Tổng CP phát hành.
- [F-03.4] Lần đầu chưa có company → hiển thị form trống để tạo mới.

**C1 — Quản lý Cuộc họp**
- [F-03.5] DS cuộc họp: bảng có filter năm + trạng thái + search tên; phân trang 20/trang.
- [F-03.6] Tạo meeting: form đầy đủ fields + 2 sub-grid.
- [F-03.7] Sửa meeting: form pre-fill + 2 sub-grid inline edit.
- [F-03.8] Xóa meeting: confirm dialog + soft-delete (set `deleted_at`) nếu chưa có shareholder; nếu đã có shareholder → block xóa + thông báo.
- [F-03.9] Nhân bản (Clone) meeting: tạo meeting mới copy metadata + resolutions + candidates.
- [F-03.10] Sub-grid Tờ trình biểu quyết: add/edit/delete nội dung; sắp xếp `display_order`.
- [F-03.11] Sub-grid Ứng viên bầu cử: Chia làm 2 DataGrid riêng biệt cho HĐQT và BKS (UI thân thiện hơn). Thêm/Sửa/Xóa ứng viên cho từng vị trí.
- [F-03.12] Audit log: ghi mọi create/update/delete meeting, resolution, candidate với user + timestamp.

### Non-Functional

- [NF-03.1] DS cuộc họp load < 1s cho 100 records.
- [NF-03.2] Tạo/sửa meeting (bao gồm save resolutions + candidates) < 2s.
- [NF-03.3] Logo upload async — không block UI thread.
- [NF-03.4] Chỉ `admin` và `operator` mới tạo/sửa/xóa meeting. `viewer` chỉ đọc.

---

## Architecture

### CQRS Command/Query Flow

```
Blazor Page
  │ IMediator.Send(command/query)
  ▼
Application Layer (Commands + Queries + Validators)
  ├── GetCompanyQuery → CompanyDto
  ├── UpsertCompanyCommand → CompanyDto
  ├── GetMeetingsQuery (filter) → PagedResult<MeetingListItemDto>
  ├── GetMeetingByIdQuery → MeetingDetailDto
  ├── CreateMeetingCommand → Guid (meetingId)
  ├── UpdateMeetingCommand → Unit
  ├── DeleteMeetingCommand → Unit
  └── CloneMeetingCommand → Guid (new meetingId)
  ▼
Infrastructure Layer (Handlers → EF Core → Postgres)
  ├── MmsDbContext
  └── AuditLogService (ghi audit_logs)
```

### Component Tree — Meeting Form

```
MeetingFormPage.razor
├── MudForm (validation)
│   ├── Thông tin cơ bản fields (MudTextField, MudSelect, MudDatePicker)
│   ├── ResolutionsGrid.razor (sub-component)
│   │   └── MudDataGrid inline editable
│   ├── CandidatesGrid.razor (sub-component position="HĐQT")
│   └── CandidatesGrid.razor (sub-component position="BKS")
└── Actions: [Lưu] [Hủy]
```

---

## Related Code Files

### Tạo mới

```
src/Mms.Application/
├── Companies/
│   ├── Queries/GetCompanyQuery.cs
│   ├── Commands/UpsertCompanyCommand.cs
│   ├── Validators/UpsertCompanyValidator.cs
│   └── Dtos/CompanyDto.cs
├── Meetings/
│   ├── Queries/
│   │   ├── GetMeetingsQuery.cs          # paged list with filters
│   │   └── GetMeetingByIdQuery.cs
│   ├── Commands/
│   │   ├── CreateMeetingCommand.cs
│   │   ├── UpdateMeetingCommand.cs
│   │   ├── DeleteMeetingCommand.cs
│   │   └── CloneMeetingCommand.cs
│   ├── Validators/
│   │   ├── CreateMeetingValidator.cs
│   │   └── UpdateMeetingValidator.cs
│   └── Dtos/
│       ├── MeetingListItemDto.cs
│       ├── MeetingDetailDto.cs
│       ├── ResolutionDto.cs
│       └── CandidateDto.cs
└── Common/
    ├── PagedResult.cs
    └── IAuditLogService.cs

src/Mms.Infrastructure/
├── Handlers/
│   ├── Companies/
│   │   ├── GetCompanyHandler.cs
│   │   └── UpsertCompanyHandler.cs
│   └── Meetings/
│       ├── GetMeetingsHandler.cs
│       ├── GetMeetingByIdHandler.cs
│       ├── CreateMeetingHandler.cs
│       ├── UpdateMeetingHandler.cs
│       ├── DeleteMeetingHandler.cs
│       └── CloneMeetingHandler.cs
└── Services/
    └── AuditLogService.cs              # implement IAuditLogService

src/Mms.Web/Pages/
├── Company/
│   └── CompanyInfoPage.razor           # B2
└── Meetings/
    ├── MeetingListPage.razor            # C1 list
    ├── MeetingFormPage.razor            # C1 form (tạo + sửa)
    └── Components/
        ├── ResolutionsGrid.razor        # sub-grid inline
        └── CandidatesGrid.razor         # sub-grid inline
```

### Sửa

```
src/Mms.Web/Shared/NavMenu.razor        # thêm link B2, C1
src/Mms.Web/Pages/Dashboard/DashboardPage.razor  # replace mock → real data
```

---

## Implementation Steps

### Bước 1: MediatR + FluentValidation Setup

1. Cài NuGet: `MediatR`, `FluentValidation.AspNetCore`, `AutoMapper.Extensions.Microsoft.DependencyInjection`.
2. `Program.cs`:
   ```csharp
   builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
       typeof(GetCompanyQuery).Assembly)); // Application assembly
   builder.Services.AddValidatorsFromAssembly(typeof(GetCompanyQuery).Assembly);
   builder.Services.AddAutoMapper(typeof(GetCompanyQuery).Assembly);
   // Pipeline behavior: validation
   builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
   ```
3. Tạo `ValidationBehavior.cs` — throw `ValidationException` nếu FluentValidation fail.

### Bước 2: Company CRUD & Migration

1. Tạo EF Migration `AddExtraFieldsToCompany`: bổ sung `Website`, `EnglishName`, `StockSymbol`, `StockExchange`, `SealImagePath`, `SignatureImagePath`.
2. Update `CompanyDto.cs` mirror fields UI spec B2 (21 fields).
2. `GetCompanyQuery` handler: `context.Companies.FirstOrDefaultAsync()` — trả null nếu chưa có.
3. `UpsertCompanyCommand` handler: INSERT nếu chưa có, UPDATE nếu đã có (singleton pattern).
4. `UpsertCompanyValidator`: MST regex `^[0-9]{10}([0-9]{3})?$`, vốn điều lệ > 0, `TotalVotingShares <= TotalSharesIssued`.
5. `CompanyInfoPage.razor` (B2):
   - `OnInitializedAsync`: `IMediator.Send(new GetCompanyQuery())` → bind vào form model.
   - Form 21 fields đúng theo UI spec.
   - `MudFileUpload` cho logo, con dấu, chữ ký → upload qua API endpoint `POST /api/uploads/image`.
   - `[Authorize(Roles="admin")]`.

### Bước 3: Meeting CRUD — List Page

1. `GetMeetingsQuery`: params `year?, status?, searchTerm?, page, pageSize`.
2. Handler: EF query với `.Where()` chaining + `.Skip().Take()` + count total.
3. `MeetingListPage.razor`:
   - `MudDataGrid` ServerData mode (server-side paging/filtering).
   - Columns: #, Tên, Loại, Ngày, Tổng CĐ (placeholder 0 cho pilot), Trạng thái badge, Actions.
   - Filter bar: `MudSelect` năm + trạng thái + `MudTextField` search.
   - Buttons: [Mở] (navigate), [Sửa] (navigate), [Nhân bản] (command), [Xóa] (confirm dialog).

### Bước 4: Meeting CRUD — Form Page (tạo + sửa)

1. Route: `@page "/meetings/new"` và `@page "/meetings/{Id:guid}/edit"`.
2. `OnInitializedAsync`:
   - Nếu `Id != null` → `GetMeetingByIdQuery` → pre-fill form.
   - Nếu tạo mới → tự fill `TotalVotingShares` từ company.
3. Form fields (đúng UI spec C1 Form):
   - Tên cuộc họp, Loại (MudSelect), Ngày họp (MudDatePicker), Giờ bắt đầu (MudTimePicker).
   - Địa điểm, Ngày chốt DS CĐ, Tổng CP BQ, Chủ tọa, Thư ký, Ghi chú.
4. `ResolutionsGrid.razor`:
   - `MudDataGrid` với `EditMode="DataGridEditMode.Cell"`.
   - Columns: STT, Tên nội dung, Chi tiết (textarea), [Xóa].
   - Nút [+ Thêm nội dung] → add row mới.
   - Drag-sort: dùng `display_order` tăng dần (drag-to-reorder optional — TODO Phase sau).
5. `CandidatesGrid.razor`:
   - Parent truyền Parameter `Position` (HĐQT hoặc BKS) để tái sử dụng Component.
   - Columns: STT, Họ tên, Chức vụ hiện tại, Năm sinh, Ghi chú, [Xóa].
6. Submit → `CreateMeetingCommand` / `UpdateMeetingCommand` (chứa lists resolutions + candidates).
7. Handler: transaction — upsert meeting → delete-and-reinsert resolutions + candidates (đơn giản nhất cho pilot).

### Bước 5: Soft Delete + Clone

1. Thêm `deleted_at TIMESTAMPTZ NULL` vào bảng `meetings` qua migration mới: `AddSoftDeleteToMeetings`.
2. `DeleteMeetingHandler`:
   - Check: `context.Shareholders.AnyAsync(s => s.MeetingId == id)` → if true → throw `BusinessException("Không thể xóa meeting đã có cổ đông")`.
   - If false → `meeting.DeletedAt = DateTime.UtcNow; context.SaveChanges()`.
3. Global query filter: `builder.HasQueryFilter(m => m.DeletedAt == null)`.
4. `CloneMeetingHandler`: copy meeting + resolutions + candidates với `Id = Guid.NewGuid()`, `Status = NEW`, `CreatedAt = now`, shareholders KHÔNG copy.

### Bước 6: Audit Log

1. `IAuditLogService.LogAsync(category, entityType, entityId, detail, userId, meetingId?)`.
2. `AuditLogService` implement: `context.AuditLogs.Add(...)` + `SaveChangesAsync()` riêng (không chung transaction với business để không rollback audit khi có lỗi).
3. Ghi audit trong mỗi handler: CREATE_MEETING, UPDATE_MEETING, DELETE_MEETING, CLONE_MEETING.

### Bước 7: Dashboard — Replace Mock Data

1. Inject `IMediator` vào `DashboardPage.razor`.
2. Gọi `GetMeetingsQuery(page=1, pageSize=5, orderByDate=desc)` → hiển thị "cuộc họp gần nhất".
3. Stats card: tổng meeting, meeting đã hoàn tất, meeting sắp tới.

---

## Todo List

- [ ] Cài MediatR, FluentValidation, AutoMapper NuGet
- [ ] Tạo ValidationBehavior pipeline
- [ ] Tạo CompanyDto + GetCompanyQuery + UpsertCompanyCommand + Validator
- [ ] Tạo CompanyInfoPage.razor (B2) với upload logo
- [ ] Tạo API endpoint POST /api/uploads/logo
- [ ] Tạo GetMeetingsQuery (paged + filtered) + Handler
- [ ] Tạo GetMeetingByIdQuery + Handler
- [ ] Tạo CreateMeetingCommand + UpdateMeetingCommand + Validators + Handlers
- [ ] Tạo DeleteMeetingCommand (soft-delete + block nếu có shareholder)
- [ ] Tạo CloneMeetingCommand + Handler
- [ ] Tạo MeetingListPage.razor (C1 list + filter + actions)
- [ ] Tạo MeetingFormPage.razor (C1 form tạo/sửa)
- [ ] Tạo ResolutionsGrid.razor (sub-component inline edit)
- [ ] Tạo CandidatesGrid.razor (sub-component inline edit)
- [ ] Migration AddSoftDeleteToMeetings
- [ ] Implement IAuditLogService + AuditLogService
- [ ] Ghi audit trong tất cả handlers
- [ ] Replace mock data trong DashboardPage
- [ ] Update NavMenu với links B2, C1
- [ ] Unit test validators (5 cases mỗi validator)
- [ ] Integration test Create + Update + Delete meeting (Testcontainers)

---

## Success Criteria

- [ ] Admin tạo meeting với 3 resolutions + 2 candidates → lưu thành công, audit log có 1 row.
- [ ] Edit meeting → sửa tên resolution → lưu → query lại đúng tên mới.
- [ ] Xóa meeting chưa có shareholder → thành công (soft-delete).
- [ ] Xóa meeting đã có shareholder → báo lỗi, không xóa.
- [ ] Clone meeting → meeting mới có đủ resolutions + candidates, status = NEW.
- [ ] DS cuộc họp filter theo năm + trạng thái hoạt động đúng.
- [ ] Dashboard thay mock → hiển thị data thật.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Delete-and-reinsert resolutions làm mất `display_order` | Medium | Sort by `display_order` trước khi reinsert; gán lại display_order = index + 1 |
| Concurrency: 2 admin sửa cùng meeting | Low | EF `DbUpdateConcurrencyException` → thông báo "Dữ liệu đã thay đổi, vui lòng tải lại" |
| Logo upload path traversal | High | Validate filename (chỉ allow [a-z0-9._-]), rename thành UUID.ext, lưu ngoài wwwroot nếu có thể |
| CORS issue khi upload API riêng | Low | Upload endpoint cùng domain (`/api/uploads/logo`) — không CORS |

---

## Security Considerations

- Logo upload: validate MIME type thực (magic bytes, không chỉ extension); giới hạn size 2MB; rename thành UUID.
- Chỉ `admin` truy cập `CompanyInfoPage` (`[Authorize(Roles="admin")]`).
- Xóa meeting: chỉ `admin` — thêm role check trong handler.
- SQL injection: không thể xảy ra với EF parameterized queries. Cẩn thận với raw SQL nếu dùng.

---

## Next Steps

- Phase-04 cần: `meeting_id` hợp lệ trong DB + bảng `shareholders` đã có UNIQUE index `(meeting_id, id_number)`.
- Phase-04 cần: `company.total_voting_shares` để validate tổng CP import ≤ VĐL.
- Phase-05 sẽ test Create Meeting flow trong Playwright E2E scenario 2.
