# Phase 06B — Quản lý Tài Khoản & Phân Quyền (User Account Management & Authorization)

## Bối cảnh

Phase 06B hoàn thiện tầng quản trị người dùng. Các route `/admin/users` và `/admin/audit-log` đã có trong NavMenu nhưng chưa có trang thực. `ApplicationUser` và `ApplicationRole` đã tồn tại với 4 roles (`admin`, `operator`, `viewer`, `checkin`). Phase này implement đủ để admin quản lý tài khoản mà không cần thao tác DB thủ công.

---

## Phân tích GAP hiện tại

| Hạng mục | Hiện tại | Yêu cầu Phase 06B |
|---|---|---|
| `/admin/users` | NavLink có, trang không tồn tại | User CRUD + Role assignment |
| `/admin/audit-log` | NavLink có, trang không tồn tại | Read-only grid, filterable |
| `/account/profile` | Không có | Xem + sửa thông tin cá nhân |
| ApplicationUser.IsActive | Không có | Thêm field, dùng Identity LockoutEnd |
| Application Layer (Users) | Không có Commands/Queries cho users | 6 Commands + 2 Queries |
| Route Authorization | `[Authorize]` có thể thiếu trên các trang | Kiểm tra và bổ sung toàn bộ |

---

## Role Matrix (Tham chiếu cho toàn dự án)

| Route | admin | operator | checkin | viewer |
|---|---|---|---|---|
| `/admin/*` | ✅ | ❌ | ❌ | ❌ |
| `/meetings`, `/meetings/*` | ✅ | ✅ | ❌ | ❌ |
| `/company` | ✅ | ❌ | ❌ | ❌ |
| `/proxy` | ✅ | ✅ | ❌ | ❌ |
| `/checkin` | ✅ | ✅ | ✅ | ❌ |
| `/tallying` | ✅ | ✅ | ❌ | ❌ |
| `/reports` | ✅ | ✅ | ❌ | ✅ |
| `/account/profile` | ✅ | ✅ | ✅ | ✅ |

---

## Domain/Entity Changes

### [MODIFY] `src/Mms.Infrastructure/Identity/ApplicationUser.cs`
Thêm field:
```csharp
public bool IsActive { get; set; } = true;
```

> Để disable user: set `IsActive = false` + `LockoutEnd = DateTimeOffset.MaxValue` + `LockoutEnabled = true`.
> Để enable: set `IsActive = true` + `LockoutEnd = null`.

Tạo EF migration: `AddUserIsActiveField`

---

## Application Layer

### Thư mục `src/Mms.Application/Users/`

#### Commands
- **`CreateUserCommand`** — tạo user mới + gán role. Fields: `UserName`, `FullName`, `Email`, `Password`, `Role` (single). Dùng `UserManager<ApplicationUser>`.
- **`UpdateUserCommand`** — cập nhật `FullName`, `Email`, `Role` (thay đổi role). Fields: `UserId`, `FullName`, `Email`, `NewRole`.
- **`ToggleUserActiveCommand`** — enable/disable user. Fields: `UserId`, `IsActive`. Logic: set `LockoutEnd` + `IsActive`.
- **`AdminResetPasswordCommand`** — admin reset password bất kỳ user. Fields: `UserId`, `NewPassword`. Dùng `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`.
- **`UpdateProfileCommand`** — user tự cập nhật `FullName`, `Email`. Fields: `UserId`, `FullName`, `Email`.
- **`ChangeOwnPasswordCommand`** — user tự đổi password (verify old password trước). Fields: `UserId`, `CurrentPassword`, `NewPassword`.

#### Queries
- **`GetUsersQuery`** — paged list users với roles. Trả `UserListItemDto { Id, UserName, FullName, Email, Role, IsActive, LastLoginAt }`.
- **`GetAuditLogsQuery`** — paged + filter. Trả `AuditLogDto { Id, EntityName, EntityId, Action, Detail, PerformedBy, CreatedAt }`. Filter: `DateFrom?`, `DateTo?`, `EntityName?`, `Action?`, `PerformedBy?`.

#### Validators (FluentValidation)
- **`CreateUserValidator`**: UserName not empty, ≥4 chars, no spaces; Password ≥8 chars, có uppercase + digit; Email valid; Role phải trong danh sách hợp lệ.
- **`UpdateUserValidator`**: Email valid format.
- **`AdminResetPasswordValidator`**: NewPassword ≥8 chars, có uppercase + digit.
- **`UpdateProfileValidator`**: FullName not empty; Email valid.
- **`ChangeOwnPasswordValidator`**: NewPassword ≥8 chars; NewPassword != CurrentPassword.

#### DTOs
```csharp
record UserListItemDto(Guid Id, string UserName, string FullName, string? Email,
                       string Role, bool IsActive, DateTime? LastLoginAt);
record AuditLogDto(long Id, string EntityName, string? EntityId, string Action,
                   string? Detail, string? PerformedBy, DateTime CreatedAt);
```

---

## Web UI

### [NEW] `src/Mms.Web/Components/Pages/Admin/user-management-page.razor`
Route: `@page "/admin/users"` + `@attribute [Authorize(Roles = "admin")]`

**Layout:**
- Header: "Quản lý Tài Khoản" + Button "➕ Tạo tài khoản" (mở dialog)
- `MudDataGrid<UserListItemDto>` server-side paging (PageSize=20):
  - Columns: Tên đăng nhập / Họ tên / Email / Vai trò / Trạng thái / Đăng nhập lần cuối / Actions
  - Status badge: Active=Success, Inactive=Error
  - Vai trò chip: admin=Error, operator=Warning, checkin=Info, viewer=Default
  - Actions (icon buttons): Sửa | Đổi mật khẩu | Bật/Tắt

**Dialog — Tạo tài khoản mới:**
- `MudTextField` UserName, FullName, Email
- `MudTextField` Password (type=password) + confirm password (validate match client-side)
- `MudSelect<string>` Role (admin / operator / viewer / checkin)
- Submit → `CreateUserCommand`

**Dialog — Sửa tài khoản:**
- `MudTextField` FullName, Email (UserName readonly)
- `MudSelect<string>` Role
- Submit → `UpdateUserCommand`

**Dialog — Reset mật khẩu:**
- `MudTextField` NewPassword (type=password)
- Submit → `AdminResetPasswordCommand`

**Toggle Active** — confirm `MudDialog` "Bạn có chắc muốn [tắt/bật] tài khoản **{UserName}**?" → `ToggleUserActiveCommand`

---

### [NEW] `src/Mms.Web/Components/Pages/Admin/audit-log-page.razor`
Route: `@page "/admin/audit-log"` + `@attribute [Authorize(Roles = "admin")]`

**Layout:**
- Filter bar: DatePicker From/To + `MudTextField` search PerformedBy + `MudSelect` EntityName (Meeting, Company, Shareholder, User...) + Button Lọc
- `MudDataGrid<AuditLogDto>` paging (PageSize=50), columns:
  - Thời gian / Thực thể / ID đối tượng / Hành động / Người thực hiện / Chi tiết
  - Column "Chi tiết": truncate 60 chars + click để xem full trong `MudTooltip`
- Read-only — không có nút edit/delete (audit log bất biến theo thiết kế)

---

### [NEW] `src/Mms.Web/Components/Pages/Account/profile-page.razor`
Route: `@page "/account/profile"` + `@attribute [Authorize]`

**Layout — 2 cards ngang:**

**Card 1 — Thông tin cá nhân:**
- Display: Avatar icon + UserName (readonly) + FullName + Email
- Button "Chỉnh sửa" → inline form mode
- Submit → `UpdateProfileCommand`

**Card 2 — Đổi mật khẩu:**
- `MudTextField` Mật khẩu hiện tại + Mật khẩu mới + Xác nhận mật khẩu
- Submit → `ChangeOwnPasswordCommand`
- Kết quả: toast success hoặc error inline (không redirect)

---

### [MODIFY] NavMenu — thêm Profile link
Thêm vào cuối NavMenu (sau tất cả group), visible với `<AuthorizeView>` (mọi role):
```razor
<MudNavLink Href="/account/profile" Icon="@Icons.Material.Filled.AccountCircle">
    Hồ sơ của tôi
</MudNavLink>
```

---

### [AUDIT] Route Authorization — kiểm tra toàn bộ pages

Kiểm tra và đảm bảo mỗi trang có `@attribute [Authorize(Roles = "...")]` đúng theo Role Matrix:

| File | Attribute cần có |
|---|---|
| `MeetingListPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `MeetingFormPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `ImportWizardPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `InvitationLettersPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `CompanyInfoPage.razor` | `[Authorize(Roles = "admin")]` |
| `DashboardPage.razor` | `[Authorize]` |

---

## Execution Order (7 Bước)

| Bước | Nội dung | Est. Time |
|------|----------|-----------|
| 1 | ApplicationUser.IsActive + EF Migration | 0.5h |
| 2 | Application Layer: Commands + Queries + Validators | 2h |
| 3 | User Management Page (`/admin/users`) | 2h |
| 4 | Audit Log Page (`/admin/audit-log`) | 1h |
| 5 | Profile Page (`/account/profile`) | 1h |
| 6 | NavMenu Profile link + Route authorization audit | 0.5h |
| 7 | Build verify + smoke test manual | 0.5h |

**Tổng ước tính: ~7.5h** (1 ngày làm việc)

---

## Verification Checklist

- [ ] `dotnet build` → 0 errors
- [ ] Migration `AddUserIsActiveField` chạy clean
- [ ] Admin login → `/admin/users` → Tạo user operator → Đăng nhập user mới → verify role
- [ ] Admin reset password → user login với password mới
- [ ] Disable user → user login → bị từ chối (401/redirect login)
- [ ] `/admin/audit-log` load danh sách, filter theo date
- [ ] User thường → `/account/profile` → đổi password → login lại OK
- [ ] User thường navigate đến `/admin/users` → redirect 403/login
- [ ] Update `docs/context_style_notes.md` thêm Phase 06B section

---

## Notes cho AI thực thi

1. **UserManager trong Application layer**: Inject `UserManager<ApplicationUser>` + `RoleManager<ApplicationRole>` vào Handlers trực tiếp (không cần repository wrapper — Identity đã là abstraction đủ).
2. **Disable user = lockout**: `user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.MaxValue; user.IsActive = false;` — sau đó `UpdateAsync(user)`. Identity sẽ từ chối login.
3. **Password validate**: Dùng `userManager.PasswordValidators` để validate password strength — không tự viết regex.
4. **`ChangeOwnPassword`**: Bắt buộc dùng `ChangePasswordAsync(user, currentPassword, newPassword)` — không dùng token flow (token flow chỉ dành cho reset qua email).
5. **Audit log**: Bảng `audit_logs` có trigger bảo vệ — chỉ INSERT, không UPDATE/DELETE. Query thẳng từ `AppDbContext.AuditLogs.AsNoTracking()`.
6. **Page role check**: `@attribute [Authorize(Roles = "admin")]` trên Blazor page ≡ redirect về `/login` nếu không có role.
7. **Dialog pattern**: Dùng `MudDialogService.ShowAsync<TComponent>()` — tham chiếu Phase 03 cách dùng đã có trong codebase.
