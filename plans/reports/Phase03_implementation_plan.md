# Phase 03 — Company Info + Meeting CRUD — Implementation Plan

## Mô tả

Triển khai 2 nhóm tính năng CRUD cốt lõi: (1) Quản lý Thông tin Doanh nghiệp với 21 trường thông tin và upload 3 ảnh (Logo, Con dấu, Chữ ký); (2) Quản lý Cuộc họp với danh sách lọc/phân trang, form tạo/sửa kèm 3 sub-grid inline (Tờ trình, Ứng viên HĐQT, Ứng viên BKS), clone và soft-delete. Toàn bộ đi qua kiến trúc CQRS MediatR với FluentValidation và Audit Log.

---

## User Review Required

> [!IMPORTANT]
> **Thay đổi Domain Entity `Company`**: Bổ sung 5 trường mới (`EnglishName`, `StockExchange`, `SealImagePath`, `SignatureImagePath`, `CurrentPosition` cho MeetingCandidate) — yêu cầu tạo EF Migration mới.

> [!WARNING]
> **AuditCategory enum**: Cần bổ sung giá trị `Meeting` và `Company` để phân loại audit log. Đây là thay đổi schema nhỏ nhưng ảnh hưởng tới bảng `audit_logs`.

> [!IMPORTANT]
> **Application layer hiện đang trống**: Chỉ có `Class1.cs` placeholder. Toàn bộ cấu trúc MediatR + CQRS + FluentValidation sẽ được thiết lập mới từ đầu trong phase này.

---

## Proposed Changes

Cây thư mục tổng quan các file sẽ sinh ra / chỉnh sửa:

```
src/
├── Mms.Domain/
│   ├── Entities/Company.cs                          [MODIFY] +5 fields
│   ├── Entities/MeetingCandidate.cs                 [MODIFY] +1 field
│   └── Enums/AuditCategory.cs                       [MODIFY] +2 values
│
├── Mms.Application/
│   ├── Class1.cs                                    [DELETE]
│   ├── Common/
│   │   ├── Behaviours/ValidationBehaviour.cs        [NEW]
│   │   ├── Exceptions/ValidationException.cs        [NEW]
│   │   ├── Interfaces/IAuditLogService.cs           [NEW]
│   │   ├── Models/PagedResult.cs                    [NEW]
│   │   └── Mappings/MappingProfile.cs               [NEW]  (Manual mapping, no AutoMapper)
│   ├── Companies/
│   │   ├── Dtos/CompanyDto.cs                       [NEW]
│   │   ├── Queries/GetCompanyQuery.cs               [NEW]
│   │   ├── Commands/UpsertCompanyCommand.cs          [NEW]
│   │   └── Validators/UpsertCompanyValidator.cs      [NEW]
│   └── Meetings/
│       ├── Dtos/
│       │   ├── MeetingListItemDto.cs                [NEW]
│       │   ├── MeetingDetailDto.cs                  [NEW]
│       │   ├── ResolutionDto.cs                     [NEW]
│       │   └── CandidateDto.cs                      [NEW]
│       ├── Queries/
│       │   ├── GetMeetingsQuery.cs                  [NEW]
│       │   └── GetMeetingByIdQuery.cs               [NEW]
│       ├── Commands/
│       │   ├── CreateMeetingCommand.cs              [NEW]
│       │   ├── UpdateMeetingCommand.cs              [NEW]
│       │   ├── DeleteMeetingCommand.cs              [NEW]
│       │   └── CloneMeetingCommand.cs               [NEW]
│       └── Validators/
│           ├── CreateMeetingValidator.cs             [NEW]
│           └── UpdateMeetingValidator.cs             [NEW]
│
├── Mms.Infrastructure/
│   ├── DependencyInjection.cs                       [MODIFY] +MediatR +Validation DI
│   ├── Persistence/
│   │   ├── Configurations/CompanyConfiguration.cs   [MODIFY] +new columns
│   │   ├── Configurations/MeetingCandidateConfig.cs [MODIFY] +CurrentPosition
│   │   └── Migrations/YYYYMMDD_AddCompanyExtraFields.cs [NEW] (auto-generated)
│   ├── Handlers/
│   │   ├── Companies/
│   │   │   ├── GetCompanyHandler.cs                 [NEW]
│   │   │   └── UpsertCompanyHandler.cs              [NEW]
│   │   └── Meetings/
│   │       ├── GetMeetingsHandler.cs                [NEW]
│   │       ├── GetMeetingByIdHandler.cs             [NEW]
│   │       ├── CreateMeetingHandler.cs              [NEW]
│   │       ├── UpdateMeetingHandler.cs              [NEW]
│   │       ├── DeleteMeetingHandler.cs              [NEW]
│   │       └── CloneMeetingHandler.cs               [NEW]
│   └── Services/
│       └── AuditLogService.cs                       [NEW]
│
├── Mms.Web/
│   ├── Program.cs                                   [MODIFY] +MediatR registration
│   ├── Api/
│   │   └── UploadsController.cs                     [NEW] POST /api/uploads/image
│   ├── Components/Layout/NavMenu.razor              [MODIFY] update links
│   └── Components/Pages/
│       ├── Dashboard/DashboardPage.razor            [MODIFY] replace mock → real data
│       ├── Company/
│       │   └── CompanyInfoPage.razor                [NEW] màn hình B2
│       └── Meetings/
│           ├── MeetingListPage.razor                [NEW] màn hình C1
│           ├── MeetingFormPage.razor                [NEW] màn hình C1.1
│           └── Components/
│               ├── ResolutionsGrid.razor            [NEW] sub-grid tờ trình
│               └── CandidatesGrid.razor             [NEW] sub-grid ứng viên (tái sử dụng cho HĐQT + BKS)
│
└── Mms.Application/Mms.Application.csproj           [MODIFY] +FluentValidation.DependencyInjectionExtensions
```

---

### Component 1 — Domain Layer Changes

#### [MODIFY] [Company.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Domain/Entities/Company.cs)

Bổ sung 5 property mới dựa trên UI B2:

| Property | Type | Mô tả |
|----------|------|-------|
| `EnglishName` | `string?` | Tên tiếng Anh của công ty |
| `StockExchange` | `string?` | Sàn niêm yết (HOSE/HNX/UPCOM) |
| `SealImagePath` | `string?` | Đường dẫn ảnh con dấu |
| `SignatureImagePath` | `string?` | Đường dẫn ảnh chữ ký đại diện |

> [!NOTE]
> `Website`, `StockCode`, `LogoPath` đã có sẵn trong Entity hiện tại. Không cần thêm.

#### [MODIFY] [MeetingCandidate.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Domain/Entities/MeetingCandidate.cs)

Bổ sung:

| Property | Type | Mô tả |
|----------|------|-------|
| `CurrentPosition` | `string?` | Chức vụ hiện tại (theo giao diện thiết kế) |

#### [MODIFY] [AuditCategory.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Domain/Enums/AuditCategory.cs)

Bổ sung 2 giá trị: `Meeting`, `Company` vào enum hiện có.

---

### Component 2 — Application Layer (Hoàn toàn mới)

#### [DELETE] Class1.cs
Xóa file placeholder.

#### [NEW] Common/ — Shared Infrastructure

| File | Mô tả |
|------|-------|
| `Behaviours/ValidationBehaviour.cs` | MediatR pipeline behavior: tự động chạy FluentValidation trước khi handler xử lý |
| `Exceptions/ValidationException.cs` | Custom exception chứa dictionary lỗi validation |
| `Interfaces/IAuditLogService.cs` | Interface cho audit logging |
| `Models/PagedResult.cs` | Generic `PagedResult<T>` với `Items`, `TotalCount`, `Page`, `PageSize` |

#### [NEW] Companies/ — Company CQRS

| File | Pattern | Mô tả |
|------|---------|-------|
| `Dtos/CompanyDto.cs` | DTO | 21 fields mirror UI form B2 |
| `Queries/GetCompanyQuery.cs` | `IRequest<CompanyDto?>` | Trả company singleton hoặc null |
| `Commands/UpsertCompanyCommand.cs` | `IRequest<CompanyDto>` | Insert nếu chưa có, Update nếu đã có |
| `Validators/UpsertCompanyValidator.cs` | `AbstractValidator` | MST regex `^[0-9]{10}([0-9]{3})?$`, VĐL > 0, CP BQ ≤ CP phát hành |

#### [NEW] Meetings/ — Meeting CQRS

| File | Pattern | Mô tả |
|------|---------|-------|
| `Dtos/MeetingListItemDto.cs` | DTO | Cho danh sách: Id, Title, MeetingDate, Status, ShareholderCount |
| `Dtos/MeetingDetailDto.cs` | DTO | Đầy đủ fields + `List<ResolutionDto>` + `List<CandidateDto>` |
| `Dtos/ResolutionDto.cs` | DTO | DisplayOrder, Title, Content |
| `Dtos/CandidateDto.cs` | DTO | DisplayOrder, FullName, Position, CurrentPosition, BirthYear, Notes |
| `Queries/GetMeetingsQuery.cs` | `IRequest<PagedResult<MeetingListItemDto>>` | Params: year?, status?, search?, page, pageSize |
| `Queries/GetMeetingByIdQuery.cs` | `IRequest<MeetingDetailDto?>` | Includes Resolutions + Candidates |
| `Commands/CreateMeetingCommand.cs` | `IRequest<Guid>` | Tạo mới meeting + resolutions + candidates |
| `Commands/UpdateMeetingCommand.cs` | `IRequest<Unit>` | Cập nhật meeting + delete-reinsert children |
| `Commands/DeleteMeetingCommand.cs` | `IRequest<Unit>` | Soft-delete, block nếu có shareholders |
| `Commands/CloneMeetingCommand.cs` | `IRequest<Guid>` | Clone metadata + resolutions + candidates, Status = New |
| `Validators/CreateMeetingValidator.cs` | `AbstractValidator` | Title required, MeetingDate > today, TotalVotingShares > 0 |
| `Validators/UpdateMeetingValidator.cs` | `AbstractValidator` | Tương tự Create |

---

### Component 3 — Infrastructure Layer (Handlers + Services)

#### [MODIFY] [DependencyInjection.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Infrastructure/DependencyInjection.cs)

Bổ sung đăng ký:
```csharp
services.AddScoped<IAuditLogService, AuditLogService>();
```

#### [MODIFY] [CompanyConfiguration.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs)

Thêm mapping cho 4 cột mới: `EnglishName`, `StockExchange`, `SealImagePath`, `SignatureImagePath`.

#### [NEW] Migration `AddCompanyExtraFields`

Tự sinh bằng `dotnet ef migrations add AddCompanyExtraFields`. Nội dung:
- `ALTER TABLE companies ADD COLUMN english_name TEXT NULL`
- `ALTER TABLE companies ADD COLUMN stock_exchange VARCHAR(10) NULL`
- `ALTER TABLE companies ADD COLUMN seal_image_path TEXT NULL`
- `ALTER TABLE companies ADD COLUMN signature_image_path TEXT NULL`
- `ALTER TABLE meeting_candidates ADD COLUMN current_position TEXT NULL`

#### [NEW] Handlers/ — 8 Handler Files

| File | Input → Output |
|------|----------------|
| `Companies/GetCompanyHandler.cs` | `GetCompanyQuery` → `CompanyDto?` (FirstOrDefault) |
| `Companies/UpsertCompanyHandler.cs` | `UpsertCompanyCommand` → `CompanyDto` (Insert or Update) |
| `Meetings/GetMeetingsHandler.cs` | `GetMeetingsQuery` → `PagedResult<MeetingListItemDto>` (EF query + Where + Skip/Take) |
| `Meetings/GetMeetingByIdHandler.cs` | `GetMeetingByIdQuery` → `MeetingDetailDto?` (Include Resolutions, Candidates) |
| `Meetings/CreateMeetingHandler.cs` | `CreateMeetingCommand` → `Guid` (single transaction) |
| `Meetings/UpdateMeetingHandler.cs` | `UpdateMeetingCommand` → `Unit` (delete children → reinsert) |
| `Meetings/DeleteMeetingHandler.cs` | `DeleteMeetingCommand` → `Unit` (check shareholders → soft delete) |
| `Meetings/CloneMeetingHandler.cs` | `CloneMeetingCommand` → `Guid` (copy + new IDs) |

#### [NEW] Services/AuditLogService.cs

```csharp
public interface IAuditLogService
{
    Task LogAsync(AuditCategory category, string entityType,
                  Guid? entityId, string detail,
                  Guid? userId, string actor,
                  Guid? meetingId = null);
}
```

Implement: `context.AuditLogs.Add(...)` + `SaveChangesAsync()` — riêng biệt transaction, không gộp chung để tránh rollback audit khi có lỗi nghiệp vụ.

---

### Component 4 — Web Layer (UI Pages)

#### [MODIFY] [Program.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Program.cs)

```csharp
// MediatR + FluentValidation pipeline
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetCompanyQuery).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(GetCompanyQuery).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
```

#### [NEW] [UploadsController.cs](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Api/UploadsController.cs)

- `POST /api/uploads/image` — nhận `IFormFile`, validate MIME (image/png, image/jpeg, image/svg+xml), max 2MB.
- Rename thành `{guid}.{ext}`, lưu vào `wwwroot/uploads/`.
- Trả về `{ path: "/uploads/{filename}" }`.
- `[Authorize(Roles = "admin")]`.

#### [NEW] [CompanyInfoPage.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Company/CompanyInfoPage.razor)

- Route: `@page "/company"`
- `@attribute [Authorize(Roles = "admin")]`
- Layout dựa theo mockup B2:
  - Section 1: Định danh & Địa điểm (Tên, Tên viết tắt, Tên tiếng Anh, MST, Mã CK, Sàn niêm yết, Địa chỉ, SĐT, Email, Fax, Website)
  - Section 2: Người đại diện (Họ tên, Chức vụ) + Upload Con dấu + Upload Chữ ký
  - Section 3: Cơ cấu vốn (VĐL, Tổng CP PH, CP có quyền BQ)
  - Section 4: Logo công ty (Upload)
  - Actions: [Hủy] [Lưu Cấu hình]

#### [NEW] [MeetingListPage.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Meetings/MeetingListPage.razor)

- Route: `@page "/meetings"`
- `@attribute [Authorize(Roles = "admin,operator")]`
- Layout dựa theo mockup C1:
  - Header: Tiêu đề + Nút [Tạo mới]
  - Filter bar: Dropdown năm, Dropdown trạng thái, Ô tìm kiếm
  - `MudDataGrid<MeetingListItemDto>` ServerData: #, Tên, Ngày ĐKCC, Tổng SH, Trạng thái (badge), Thao tác (Xem/Sửa/Nhân bản/Xóa)
  - Pagination footer

#### [NEW] [MeetingFormPage.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Meetings/MeetingFormPage.razor)

- Route: `@page "/meetings/new"` + `@page "/meetings/{Id:guid}/edit"`
- `@attribute [Authorize(Roles = "admin,operator")]`
- Layout dựa theo mockup C1.1:
  - Card chính: Tên, Loại (Dropdown), Địa điểm, Ngày họp, Giờ bắt đầu, Ngày chốt DS CĐ, Tổng CP BQ, Chủ tọa, Thư ký
  - 3 Sub-grids song song (Bento layout):
    - `<ResolutionsGrid />` — bên trái
    - `<CandidatesGrid Position="HĐQT" />` — bên phải trên
    - `<CandidatesGrid Position="BKS" />` — bên phải dưới
  - Card Ghi chú (textarea)
  - Actions: [Hủy] [Lưu]

#### [NEW] [ResolutionsGrid.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Meetings/Components/ResolutionsGrid.razor)

- Sub-component nhận `@bind-Items="List<ResolutionDto>"` (two-way binding)
- `MudDataGrid` inline editable: STT, Nội dung tờ trình, Thao tác (Sửa/Xóa)
- Nút [+] thêm dòng mới
- Empty state: "Chưa có tờ trình nào. Bấm + để thêm."

#### [NEW] [CandidatesGrid.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Meetings/Components/CandidatesGrid.razor)

- Sub-component nhận `[Parameter] string Position` ("HĐQT" hoặc "BKS") + `@bind-Items`
- Tiêu đề tự động: `$"Danh sách ứng viên bầu {Position}"`
- `MudDataGrid` inline editable: STT, Họ tên, Chức vụ hiện tại, Thao tác (Sửa/Xóa)
- Nút [+] thêm dòng mới
- Empty state: "Chưa có ứng viên nào. Bấm + để thêm."

#### [MODIFY] [NavMenu.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/NavMenu.razor)

NavMenu đã có sẵn link `/meetings` và `/company` — chỉ cần verify là đúng route.

#### [MODIFY] [DashboardPage.razor](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Dashboard/DashboardPage.razor)

- Inject `IMediator` thay cho mock data.
- Gọi `GetMeetingsQuery(page=1, pageSize=5)` → hiển thị cuộc họp gần nhất.
- Stats cards: Tổng meeting, Meeting đang hoạt động, Meeting đã hoàn thành.
- Cập nhật hardcoded màu cũ `#1a3a5c` → dùng `Color.Primary` theo Design System mới.

---

### Component 5 — NuGet & csproj Changes

#### [MODIFY] [Mms.Application.csproj](file:///D:/PROJECT/Robotia_AGM_Voting/src/Mms.Application/Mms.Application.csproj)

```xml
<!-- Đã có sẵn MediatR + FluentValidation, chỉ cần thêm: -->
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
```

> [!NOTE]
> **Không dùng AutoMapper**. Manual mapping giữ code đơn giản cho pilot, dễ debug hơn so với convention-based mapping. Mỗi Handler tự map Entity → DTO bằng object initializer.

---

## Open Questions

> [!IMPORTANT]
> **Q1**: File upload hiện lưu vào `wwwroot/uploads/` trên filesystem. Trong Docker container, dữ liệu sẽ mất khi recreate. Cần mount volume `./uploads:/app/wwwroot/uploads` trong `docker-compose.yml`. Tiến hành luôn trong Phase này?

> [!NOTE]
> **Q2**: Giao diện mockup C1.1 có khu vực "Ghi chú" ở cuối form. Entity `Meeting` đã có property `Notes` — sẽ map trực tiếp.

---

## Verification Plan

### Automated Tests

```bash
# 1. Build toàn solution (0 errors, 0 warnings)
dotnet build Mms.sln

# 2. Chạy Migration thành công
dotnet ef database update --project src/Mms.Infrastructure --startup-project src/Mms.Web

# 3. Integration tests Phase 01 vẫn pass
dotnet test tests/Mms.IntegrationTests

# 4. Unit tests cho validators (sẽ viết trong phase này)
dotnet test tests/Mms.UnitTests
```

### Manual Verification (Trình duyệt)

1. Truy cập `/company` → form trống (lần đầu) → điền 21 fields → upload logo + dấu + chữ ký → [Lưu] → reload → data đúng
2. Truy cập `/meetings` → danh sách trống → [Tạo mới] → điền form + thêm 2 tờ trình + 1 ứng viên HĐQT + 1 ứng viên BKS → [Lưu] → về danh sách thấy 1 dòng
3. [Sửa] meeting → đổi tên → [Lưu] → verify tên mới
4. [Nhân bản] → meeting mới xuất hiện với Status = New
5. [Xóa] meeting chưa có shareholder → thành công
6. Dashboard hiển thị data thực từ database
