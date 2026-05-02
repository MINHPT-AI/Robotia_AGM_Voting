# Phase 06B Execution Prompt — Quản lý Tài Khoản & Phân Quyền

Bạn là AI thực thi Phase 06B của dự án MMS (AGM Voting System).
Dự án dùng .NET 8 / Blazor Server / MudBlazor v9 / Clean Architecture / CQRS + MediatR + FluentValidation + PostgreSQL + ASP.NET Core Identity.

**Work context:** `D:/PROJECT/Robotia_AGM_Voting`
**Plan file:** `plans/reports/phase-06b-user-account-management-and-authorization-implementation-plan.md`
**Context notes:** `docs/context_style_notes.md`

Đọc plan file và context notes trước. Implement theo thứ tự 7 bước. Chạy `dotnet build` sau mỗi bước.

---

## Context nhanh (đọc trước khi code)

Đã có sẵn:
- `ApplicationUser : IdentityUser<Guid>` tại `src/Mms.Infrastructure/Identity/ApplicationUser.cs` — có `FullName`, `MustChangePassword`, `LastLoginAt`
- `ApplicationRole : IdentityRole<Guid>` tại `src/Mms.Infrastructure/Identity/ApplicationRole.cs`
- 4 roles seeded: `admin`, `operator`, `viewer`, `checkin` (xem `SeedData.cs`)
- NavMenu đã có link `/admin/users` và `/admin/audit-log` — **chưa có trang**
- `Login.razor`, `ChangePassword.razor` đã tồn tại ở `src/Mms.Web/Components/Pages/Auth/`

**Không** viết lại login flow. **Không** thêm email confirmation. **Không** thêm 2FA.

---

## BƯỚC 1 — ApplicationUser.IsActive + EF Migration

### [MODIFY] `src/Mms.Infrastructure/Identity/ApplicationUser.cs`

Thêm một field:

```csharp
public bool IsActive { get; set; } = true;
```

> **Logic disable user (dùng xuyên suốt phase này):**
> - Disable: `user.IsActive = false` + `user.LockoutEnabled = true` + `user.LockoutEnd = DateTimeOffset.MaxValue`
> - Enable: `user.IsActive = true` + `user.LockoutEnd = null`
> - Sau đó gọi `await _userManager.UpdateAsync(user)`
> - Identity tự động từ chối login khi `LockoutEnd > UtcNow`

Tạo EF Migration:

```bash
dotnet ef migrations add AddUserIsActiveField \
  --project src/Mms.Infrastructure \
  --startup-project src/Mms.Web
```

---

## BƯỚC 2 — Application Layer: Commands + Queries + Validators

### Thư mục: `src/Mms.Application/Users/`

---

### File: `src/Mms.Application/Users/Dtos/user-dtos.cs`

```csharp
public record UserListItemDto(
    Guid Id, string UserName, string FullName, string? Email,
    string Role, bool IsActive, DateTime? LastLoginAt);

public record AuditLogDto(
    long Id, string EntityName, string? EntityId, string Action,
    string? Detail, string? PerformedBy, DateTime CreatedAt);
```

---

### File: `src/Mms.Application/Users/Commands/user-commands.cs`

6 Commands trong 1 file, mỗi Command là 1 record + 1 Handler class:

**`CreateUserCommand`**
```csharp
public record CreateUserCommand(
    string UserName, string FullName, string? Email,
    string Password, string Role) : IRequest<Guid>;
```
Handler logic:
1. `new ApplicationUser { UserName, FullName, Email, MustChangePassword = false, IsActive = true }`
2. `await _userManager.CreateAsync(user, command.Password)` — throw nếu fail
3. `await _userManager.AddToRoleAsync(user, command.Role)`
4. Return `user.Id`

**`UpdateUserCommand`**
```csharp
public record UpdateUserCommand(
    Guid UserId, string FullName, string? Email, string NewRole) : IRequest;
```
Handler logic:
1. Load user bằng `FindByIdAsync`
2. Update `FullName`, `Email`
3. Lấy roles hiện tại → `RemoveFromRolesAsync` → `AddToRoleAsync(NewRole)`
4. `UpdateAsync(user)`

**`ToggleUserActiveCommand`**
```csharp
public record ToggleUserActiveCommand(Guid UserId, bool SetActive) : IRequest;
```
Handler logic:
- Nếu `SetActive = false`: `user.IsActive = false; user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.MaxValue`
- Nếu `SetActive = true`: `user.IsActive = true; user.LockoutEnd = null`
- `await _userManager.UpdateAsync(user)`

**`AdminResetPasswordCommand`**
```csharp
public record AdminResetPasswordCommand(Guid UserId, string NewPassword) : IRequest;
```
Handler logic:
1. `var token = await _userManager.GeneratePasswordResetTokenAsync(user)`
2. `var result = await _userManager.ResetPasswordAsync(user, token, command.NewPassword)`
3. Throw `ValidationException` nếu result failed

**`UpdateProfileCommand`**
```csharp
public record UpdateProfileCommand(Guid UserId, string FullName, string? Email) : IRequest;
```
Handler: update `FullName`, `Email` → `UpdateAsync`

**`ChangeOwnPasswordCommand`**
```csharp
public record ChangeOwnPasswordCommand(
    Guid UserId, string CurrentPassword, string NewPassword) : IRequest;
```
Handler: `await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword)` — throw `ValidationException` nếu CurrentPassword sai.

> **Quan trọng:** Tất cả handlers inject `UserManager<ApplicationUser>`. Không inject DbContext trực tiếp cho user operations.

---

### File: `src/Mms.Application/Users/Queries/user-queries.cs`

**`GetUsersQuery`**
```csharp
public record GetUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserListItemDto>>;
```
Handler logic:
```csharp
var users = await _userManager.Users
    .OrderBy(u => u.UserName)
    .Skip((query.Page - 1) * query.PageSize)
    .Take(query.PageSize)
    .ToListAsync();

// Lấy role cho mỗi user (N+1 OK vì PageSize nhỏ)
var items = new List<UserListItemDto>();
foreach (var u in users)
{
    var roles = await _userManager.GetRolesAsync(u);
    items.Add(new UserListItemDto(u.Id, u.UserName!, u.FullName,
        u.Email, roles.FirstOrDefault() ?? "", u.IsActive, u.LastLoginAt));
}
```

**`GetAuditLogsQuery`**
```csharp
public record GetAuditLogsQuery(
    int Page = 1, int PageSize = 50,
    DateOnly? DateFrom = null, DateOnly? DateTo = null,
    string? EntityName = null, string? PerformedBy = null)
    : IRequest<PagedResult<AuditLogDto>>;
```
Handler: query `_db.AuditLogs.AsNoTracking()` với các filter, map sang `AuditLogDto`, trả `PagedResult`.

> Dùng `PagedResult<T>` record đã có trong codebase (kiểm tra `src/Mms.Application/Common/`), hoặc tạo mới:
> ```csharp
> public record PagedResult<T>(IList<T> Items, int TotalCount, int Page, int PageSize);
> ```

---

### File: `src/Mms.Application/Users/Validators/user-validators.cs`

```csharp
// CreateUserValidator
RuleFor(x => x.UserName).NotEmpty().MinimumLength(4).Matches(@"^\S+$").WithMessage("Tên đăng nhập không được chứa khoảng trắng");
RuleFor(x => x.Password).NotEmpty().MinimumLength(8).Matches(@"[A-Z]").WithMessage("Cần ít nhất 1 chữ hoa").Matches(@"\d").WithMessage("Cần ít nhất 1 chữ số");
RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
RuleFor(x => x.Role).Must(r => new[]{"admin","operator","viewer","checkin"}.Contains(r)).WithMessage("Vai trò không hợp lệ");

// AdminResetPasswordValidator
RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).Matches(@"[A-Z]").Matches(@"\d");

// UpdateProfileValidator
RuleFor(x => x.FullName).NotEmpty();
RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

// ChangeOwnPasswordValidator
RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).Matches(@"[A-Z]").Matches(@"\d");
RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");
```

---

## BƯỚC 3 — User Management Page

### File mới: `src/Mms.Web/Components/Pages/Admin/user-management-page.razor`

```razor
@page "/admin/users"
@attribute [Authorize(Roles = "admin")]
@rendermode InteractiveServer
```

**Layout page:**

```
[Header: "Quản lý Tài Khoản"]  [Button: ➕ Tạo tài khoản]
─────────────────────────────────────────────────────────
MudDataGrid<UserListItemDto>  (server-side paging, PageSize=20)
  Columns:
    - Tên đăng nhập (sortable)
    - Họ tên
    - Email
    - Vai trò  → MudChip Color: admin=Error, operator=Warning, checkin=Info, viewer=Default
    - Trạng thái → MudChip: Active=Success "Hoạt động", Inactive=Error "Đã tắt"
    - Đăng nhập lần cuối (format "dd/MM/yyyy HH:mm", null → "Chưa đăng nhập")
    - Hành động → 3 icon buttons: ✏️ Sửa | 🔑 Đặt lại MK | 🔒/🔓 Bật/Tắt
```

**3 Dialogs** (dùng `MudDialogService` pattern giống Phase 03):

**Dialog Tạo tài khoản** (`create-user-dialog.razor`):
- Fields: UserName, FullName, Email, Password (type=password), ConfirmPassword (validate match), Role (MudSelect)
- Validate ConfirmPassword === Password trước khi Submit
- Submit → `ISender.Send(new CreateUserCommand(...))`
- Success → close dialog + reload grid

**Dialog Sửa tài khoản** (`edit-user-dialog.razor`):
- Parameter: `UserListItemDto User`
- Fields: FullName (pre-fill), Email (pre-fill), Role (MudSelect, pre-select)
- UserName hiển thị readonly
- Submit → `ISender.Send(new UpdateUserCommand(...))`

**Dialog Đặt lại mật khẩu** (`reset-password-dialog.razor`):
- Parameter: `Guid UserId`, `string UserName`
- Fields: NewPassword (type=password), ConfirmNewPassword
- Submit → `ISender.Send(new AdminResetPasswordCommand(...))`

**Toggle Active** — không dùng dialog component riêng, dùng `MudDialogService.ShowMessageBox()`:
```csharp
var confirm = await _dialogService.ShowMessageBox(
    "Xác nhận",
    $"Bạn có chắc muốn {(item.IsActive ? "TẮT" : "BẬT")} tài khoản {item.UserName}?",
    yesText: "Xác nhận", cancelText: "Huỷ");
if (confirm == true)
    await _sender.Send(new ToggleUserActiveCommand(item.Id, !item.IsActive));
```

---

## BƯỚC 4 — Audit Log Page

### File mới: `src/Mms.Web/Components/Pages/Admin/audit-log-page.razor`

```razor
@page "/admin/audit-log"
@attribute [Authorize(Roles = "admin")]
@rendermode InteractiveServer
```

**Layout:**

```
[Filter bar]
  MudDatePicker "Từ ngày"  MudDatePicker "Đến ngày"
  MudTextField "Người thực hiện"
  MudSelect "Thực thể" (Meeting, Company, Shareholder, InvitationLetter, User, ...)
  [Button: 🔍 Lọc]  [Button: Xoá lọc]

MudDataGrid<AuditLogDto>  (server-side paging, PageSize=50, read-only)
  Columns:
    - Thời gian (format "dd/MM/yyyy HH:mm:ss")
    - Thực thể (EntityName)
    - ID đối tượng (EntityId, monospace font)
    - Hành động (Action) → MudChip: Create=Success, Update=Info, Delete=Error
    - Người thực hiện
    - Chi tiết → text truncate 80 chars + MudTooltip full text on hover
```

> Không có nút sửa/xoá — audit log bất biến theo thiết kế (trigger SQL bảo vệ ở DB level).

---

## BƯỚC 5 — Profile Page

### File mới: `src/Mms.Web/Components/Pages/Account/profile-page.razor`

```razor
@page "/account/profile"
@attribute [Authorize]
@rendermode InteractiveServer
```

**Layout — 2 MudCard song song (MudGrid xs=12 md=6):**

**Card 1 — Thông tin cá nhân:**
- Display mode: Avatar icon (MudAvatar) + UserName (text, readonly) + FullName + Email
- Button "✏️ Chỉnh sửa" → toggle edit mode
- Edit mode: MudTextField FullName + MudTextField Email + [Lưu] [Huỷ]
- Submit → `UpdateProfileCommand` dùng `ClaimsPrincipal` lấy UserId hiện tại
- Lấy UserId: `_userManager.GetUserId(HttpContext.User)` hoặc `AuthenticationStateProvider`

**Card 2 — Đổi mật khẩu:**
- MudTextField "Mật khẩu hiện tại" (type=password)
- MudTextField "Mật khẩu mới" (type=password)
- MudTextField "Xác nhận mật khẩu mới" (type=password, validate match)
- Button [Đổi mật khẩu]
- Submit → `ChangeOwnPasswordCommand`
- Success: toast "Đổi mật khẩu thành công", clear form
- Error: inline MudAlert dưới form (không redirect)

> Không redirect sau đổi password — khác với `ChangePassword.razor` (trang force-change lần đầu).

---

## BƯỚC 6 — NavMenu + Route Authorization Audit

### [MODIFY] `src/Mms.Web/Components/Layout/NavMenu.razor`

Thêm link "Hồ sơ của tôi" vào cuối NavMenu, **ngoài** mọi `AuthorizeView` role-specific (hiển thị cho tất cả user đã đăng nhập):

```razor
@* Tất cả user đã đăng nhập *@
<AuthorizeView>
    <Authorized>
        <MudNavLink Href="/account/profile"
                    Icon="@Icons.Material.Filled.AccountCircle">
            Hồ sơ của tôi
        </MudNavLink>
    </Authorized>
</AuthorizeView>
```

### Route Authorization Audit

Kiểm tra từng file sau, đảm bảo có đúng `@attribute [Authorize(...)]`:

| File | Attribute đúng |
|---|---|
| `Pages/Meetings/MeetingListPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `Pages/Meetings/MeetingFormPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `Pages/Shareholders/ImportWizardPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `Pages/Meetings/InvitationLettersPage.razor` | `[Authorize(Roles = "admin,operator")]` |
| `Pages/Company/CompanyInfoPage.razor` | `[Authorize(Roles = "admin")]` |
| `Pages/Dashboard/DashboardPage.razor` | `[Authorize]` |

Nếu trang nào thiếu attribute → thêm vào. Nếu đã có nhưng sai role → sửa lại.

---

## BƯỚC 7 — Build Verify + Smoke Test

```bash
# 1. Build sạch
dotnet build --configuration Release

# 2. Unit tests không regression
dotnet test tests/Mms.UnitTests/ --configuration Release

# 3. Integration tests không regression
dotnet test tests/Mms.IntegrationTests/ --configuration Release
```

**Manual smoke test (docker-compose up -d):**
1. Đăng nhập admin → `/admin/users` → Tạo user `operator1` với role `operator`
2. Logout → Login `operator1` → verify vào được `/meetings`, không vào được `/admin/users`
3. Admin → Reset password `operator1` → Login lại với password mới → OK
4. Admin → Tắt `operator1` → Login `operator1` → bị từ chối
5. Admin → Bật lại `operator1` → Login → OK
6. `/admin/audit-log` → có entries từ các actions trên
7. `/account/profile` → Sửa FullName → Đổi password → Login lại OK

---

## Checklist Hoàn Thành

- [ ] Migration `AddUserIsActiveField` chạy clean
- [ ] `dotnet build` → 0 errors, 0 warnings mới
- [ ] Tạo user qua UI → user xuất hiện trong grid
- [ ] Disable user → login bị từ chối → Enable → login OK
- [ ] Audit log page load, filter theo date hoạt động
- [ ] Profile page: sửa FullName + đổi password thành công
- [ ] User `operator` không truy cập được `/admin/users` (redirect login/403)
- [ ] NavMenu hiển thị "Hồ sơ của tôi" cho mọi role
- [ ] Update `docs/context_style_notes.md` thêm Phase 06B section

**Report cuối:** `DONE` hoặc `DONE_WITH_CONCERNS` theo orchestration protocol.

> **Không viết unit test trong Phase 06B** — coverage bổ sung ở phase sau.
