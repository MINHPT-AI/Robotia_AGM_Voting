# Phase 02 — Blazor Server UI Shell: Login, Dashboard & CI/CD

## Background

Phase 01 hoàn thành: Database schema, Identity (BCrypt + JWT), Seed Data, Migration, API auth endpoints. Hiện tại `Mms.Web` vẫn là Blazor Server template mặc định (Home, Counter, Weather) với Bootstrap. Phase 02 chuyển đổi toàn bộ sang MudBlazor shell với auth flow + role-based navigation.

> [!IMPORTANT]
> **Quyết định kiến trúc Auth:** Spec ghi rõ dùng **Cookie Auth** cho Blazor Server (không JWT trên browser). Cần chuyển đổi từ JWT-only sang Cookie scheme — `SignInManager.PasswordSignInAsync` + `HttpContext.SignInAsync`. JWT giữ lại cho PrintAgent API sau này.

---

## Proposed Changes

### Component 1: Auth Infrastructure — Cookie Authentication

Phase 01 setup JWT-only auth. Phase 02 cần thêm Cookie scheme song song cho Blazor UI.

#### [MODIFY] [Program.cs](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Program.cs)
- Thêm `builder.Services.AddHttpContextAccessor()` (cần cho auth controller cookie sign-in)
- Thêm `builder.Services.ConfigureApplicationCookie(opts => { opts.LoginPath = "/login"; })` 
- Thêm security headers middleware: `X-Frame-Options`, `X-Content-Type-Options`

#### [MODIFY] [AuthController.cs](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Api/AuthController.cs)
- Login endpoint: sau `PasswordSignInAsync` thành công → tạo `ClaimsPrincipal` → `HttpContext.SignInAsync` với Cookie scheme (để Blazor Server nhận diện user)
- Logout endpoint: gọi `HttpContext.SignOutAsync` + xóa cookie
- Giữ JWT response song song (cho PrintAgent API dùng sau)

#### [NEW] [CookieAuthController.cs](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Api/CookieAuthController.cs)
- Tạo controller riêng cho Cookie-based login/logout (POST form từ Blazor pages)
- Login: validate credentials → `HttpContext.SignInAsync` với claims (Id, UserName, roles, MustChangePassword)
- Logout: `HttpContext.SignOutAsync` → redirect `/login`
- Change-password: validate + update Identity + clear MustChangePassword flag

---

### Component 2: MudBlazor Theme & App Shell

#### [MODIFY] [App.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/App.razor)
- Xóa Bootstrap CSS link
- Thêm MudBlazor CSS (`_content/MudBlazor/MudBlazor.min.css`)
- Thêm MudBlazor JS (`_content/MudBlazor/MudBlazor.min.js`)
- Thêm local Roboto font (self-hosted, LAN offline compatible)
- Thêm custom `app.css` overrides

#### [MODIFY] [_Imports.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/_Imports.razor)
- Thêm `@using MudBlazor`
- Thêm `@using Microsoft.AspNetCore.Components.Authorization`
- Thêm `@using Microsoft.AspNetCore.Authorization`

#### [MODIFY] [Routes.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Routes.razor)
- Wrap Router trong `<CascadingAuthenticationState>`
- Đổi `<RouteView>` → `<AuthorizeRouteView>` với `<NotAuthorized>` → redirect `/login`
- Thêm `<NotFound>` → render custom NotFound component

#### [NEW] [AppTheme.cs](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/AppTheme.cs)
- `MudTheme` custom palette: Primary `#1a3a5c` (navy), Secondary `#c9a84c` (gold)
- Typography: Roboto, 14px base
- Dark palette variant

#### [NEW] [RedirectToLogin.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Shared/RedirectToLogin.razor)
- Component dùng `NavigationManager.NavigateTo("/login", forceLoad: true)`

---

### Component 3: Layouts

#### [MODIFY] [MainLayout.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/MainLayout.razor)
- Chuyển hoàn toàn sang MudBlazor: `MudThemeProvider`, `MudDialogProvider`, `MudSnackbarProvider`
- `MudLayout` + `MudAppBar` (logo, system name, user dropdown) + `MudDrawer` (sidebar)
- User dropdown menu: Đổi mật khẩu, Đăng xuất

#### [MODIFY] [MainLayout.razor.css](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/MainLayout.razor.css)
- Xóa Bootstrap CSS, viết lại cho MudBlazor layout

#### [NEW] [EmptyLayout.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/EmptyLayout.razor)
- Layout trống (chỉ `MudThemeProvider` + centered `@Body`) — cho Login/ForgotPassword pages

#### [MODIFY] [NavMenu.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/NavMenu.razor)
- Chuyển sang `MudNavMenu` + `MudNavLink` + `MudNavGroup`
- Role-based visibility dùng `<AuthorizeView Roles="...">`
  - admin: full menu (Dashboard, Công ty, Cuộc họp, Import, Template, Ủy quyền, Check-in, Thẩm tra, Kiểm phiếu, Báo cáo, Admin group)
  - operator: Dashboard, Cuộc họp, Ủy quyền, Check-in, Kiểm phiếu, Báo cáo
  - viewer: Dashboard, Báo cáo
  - checkin: Check-in only

#### [MODIFY] [NavMenu.razor.css](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Layout/NavMenu.razor.css)
- Xóa Bootstrap styles, sử dụng MudBlazor xử lý

---

### Component 4: Auth Pages

#### [NEW] [Login.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Auth/Login.razor)
- `@page "/login"` + `@layout EmptyLayout` 
- MudCard centered: username + password + submit button
- POST form → CookieAuthController → cookie sign-in
- Error: `MudSnackbar` "Tên đăng nhập hoặc mật khẩu không đúng"
- MustChangePassword → redirect `/change-password`

#### [NEW] [ForgotPassword.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Auth/ForgotPassword.razor)
- `@page "/forgot-password"` + `@layout EmptyLayout`
- Form email → stub toast "Nếu email tồn tại, link phục hồi đã được gửi."

#### [NEW] [ChangePassword.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Auth/ChangePassword.razor)
- `@page "/change-password"` + `[Authorize]`
- 3 fields: mật khẩu cũ, mới, xác nhận
- Validation: mới ≠ cũ, xác nhận = mới, min 8 chars
- POST → CookieAuthController → update Identity + MustChangePassword=false → redirect `/`

---

### Component 5: Dashboard & Error Pages

#### [NEW] [DashboardPage.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Dashboard/DashboardPage.razor)
- `@page "/"` + `[Authorize]`
- 3 MudCard summary: Cuộc họp tổng, Cổ đông đã kiểm tra, Phiếu bầu đã in (mock data)
- `MudDataGrid` "Cuộc họp gần nhất" (1-2 mock rows)
- `MudDataGrid` "Lịch sử cuộc họp" (mock data)
- `// TODO Phase-03: inject IMeetingService` comments

#### [NEW] [NotFound.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/NotFound.razor)
- Friendly 404 page, link về Dashboard

#### [MODIFY] [Error.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Error.razor)
- Ẩn stack trace khi production
- Friendly error UI dùng MudBlazor

#### [DELETE] [Counter.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Counter.razor)
#### [DELETE] [Weather.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Weather.razor)
#### [DELETE] [Home.razor](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/Components/Pages/Home.razor)

---

### Component 6: Static Assets

#### [MODIFY] [app.css](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/wwwroot/app.css)
- Xóa Bootstrap-specific styles, viết lại cho MudBlazor base
- Custom scrollbar, font-face cho Roboto local

#### [NEW] Roboto font files
- Download Roboto (Regular, Medium, Bold) → `wwwroot/fonts/`
- `@font-face` declarations trong `app.css`

#### [DELETE] [bootstrap/](file:///d:/PROJECT/Robotia_AGM_Voting/src/Mms.Web/wwwroot/bootstrap/)
- Không cần Bootstrap khi dùng MudBlazor

---

### Component 7: CI/CD

#### [NEW] [ci-build-test.yml](file:///d:/PROJECT/Robotia_AGM_Voting/.github/workflows/ci-build-test.yml)
- Trigger: push main/develop, PR to main
- Jobs: `dotnet restore` → `build` → `test --filter Category!=E2E`
- Docker smoke build: `docker build -f docker/blazor-app.Dockerfile`

---

## Verification Plan

### Automated Tests
```bash
# Build check
dotnet build Mms.sln

# Existing integration tests still pass
dotnet test tests/Mms.IntegrationTests
```

### Manual Verification (Docker Desktop)
1. `docker-compose up -d` → navigate `http://localhost:8080`
2. Verify redirect → `/login`
3. Login `admin/Admin@2026!` → redirect `/change-password` (MustChangePassword=true)
4. Change password → redirect `/` (Dashboard)
5. Logout → redirect `/login`, browser back blocked
6. Verify NavMenu role-based visibility
7. Navigate invalid URL → custom 404 page

### Browser Subagent Tests
- Screenshot Login page
- Screenshot Dashboard after login
- Verify NavMenu rendering
