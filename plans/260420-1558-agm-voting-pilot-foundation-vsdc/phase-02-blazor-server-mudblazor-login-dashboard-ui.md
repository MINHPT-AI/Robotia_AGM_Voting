# Phase 02 — Blazor Server + MudBlazor: Login, Dashboard UI & CI/CD

## Context Links

- Parent plan: [`./plan.md`](./plan.md)
- Dependency: [`./phase-01-database-auth-identity.md`](./phase-01-database-auth-identity.md) (Identity + JWT phải xong)
- Brainstorm: [`../reports/brainstorm-260420-1558-agm-voting-system-architecture.md`](../reports/brainstorm-260420-1558-agm-voting-system-architecture.md) § 3.1 (Stack), § 3.7 (SignalR Hubs)
- UI Spec sections:
  - A1 Login: `ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md` §A1
  - A2 Forgot Password: §A2
  - A3 Change Password: §A3
  - B1 Dashboard: §B1

---

## Overview

- **Tuần**: 3
- **Priority**: P1 (unblock tất cả UI phase sau)
- **Status**: pending
- **Brief**: Dựng toàn bộ Blazor Server shell: MudBlazor theme, MainLayout, NavMenu phân quyền, 3 Auth pages (Login/ForgotPassword/ChangePassword), Dashboard skeleton (B1). Thêm CI/CD GitHub Actions skeleton (build + test). Phase này là "framework" — mọi page sau chỉ cần thêm nội dung vào shell này.

---

## Key Insights

- **Blazor Server + Cookie Auth**: dùng `<CascadingAuthenticationState>` wrap toàn bộ app. Không cần JWT trên browser — chỉ cần cookie session (server giữ state). JWT chỉ dùng cho `Print Agent` API call sau này.
- **MudBlazor**: component library đã bao gồm form validation, data grid, dialog, snackbar — tránh viết lại. Dùng `MudThemeProvider` + custom theme (màu corporate: navy/gold thường thấy ở báo cáo tài chính VN).
- **`[Authorize]` trên Blazor**: dùng `@attribute [Authorize(Roles="admin,operator")]` tại top của `.razor` page. MainLayout tự redirect `/login` nếu unauthorized.
- **NavMenu role-based**: dùng `<AuthorizeView Roles="admin">` để ẩn menu item theo role.
- **Dashboard B1 data**: phase này dùng mock data (hardcode 1 record meeting placeholder). Phase-03 sẽ inject service thật. Đây là YAGNI — không over-engineer dashboard trước khi có data model.
- **CI/CD**: GitHub Actions chỉ cần `dotnet build` + `dotnet test` (unit tests). Docker build image (smoke) cũng nên có. Playwright E2E để ở phase-05.

---

## Requirements

### Functional

- [F-02.1] `http://localhost:8080` → redirect tới `/login` nếu chưa đăng nhập.
- [F-02.2] A1 Login: submit đúng `admin/Admin@2026!` → redirect `/` (Dashboard). Sai → toast error.
- [F-02.3] A1 Login: `MustChangePassword=true` → redirect `/change-password`.
- [F-02.4] A2 Forgot Password: nhập email → hiện "Email đã gửi (nếu tồn tại)" — stub, chưa send thật.
- [F-02.5] A3 Change Password: nhập mật khẩu cũ + mới + xác nhận → validate → update Identity + `MustChangePassword=false`.
- [F-02.6] B1 Dashboard: hiển thị cards (cuộc họp / thống kê / cấu hình), bảng lịch sử mock, menu cấu hình sidebar.
- [F-02.7] NavMenu: ẩn/hiện menu items theo role (admin thấy đủ; checkin chỉ thấy Check-in; viewer chỉ thấy Dashboard + Report).
- [F-02.8] Logout button → clear session → redirect `/login`.
- [F-02.9] Error page 404 + 500 tùy chỉnh (không dùng default Blazor error UI).

### Non-Functional

- [NF-02.1] Login page load < 500ms sau khi Blazor connected.
- [NF-02.2] GitHub Actions CI build pass trên push to `main`/`develop`.
- [NF-02.3] Không leak thông tin nhạy cảm trên error page (stack trace ẩn với non-admin).
- [NF-02.4] MudBlazor bundle JS/CSS load < 2s trên LAN localhost.

---

## Architecture

### Blazor Server Layout Tree

```
App.razor
└── Router
    ├── Found → RouteView
    │   └── MainLayout.razor             ← shell chính
    │       ├── MudThemeProvider
    │       ├── MudDialogProvider
    │       ├── MudSnackbarProvider
    │       ├── NavMenu.razor            ← sidebar phân quyền
    │       └── @Body                   ← nội dung page
    └── NotFound → CustomNotFound.razor
```

### Auth Redirect Flow

```
Request /any-page
    │
    ▼
AuthorizeRouteView
    │
    ├─ Not Authenticated ──► /login
    │
    ├─ Authenticated + MustChangePassword ──► /change-password
    │
    └─ Authenticated + role OK ──► render page
```

### NavMenu Role-Based

```
Role: admin     → Dashboard, Công ty, Cuộc họp, Import VSDC, Template,
                  Ủy quyền, Check-in, Thẩm tra, Kiểm phiếu, Báo cáo,
                  [Admin] Tài khoản, Máy in, Audit Log
Role: operator  → Dashboard, Cuộc họp, Ủy quyền, Check-in, Kiểm phiếu, Báo cáo
Role: viewer    → Dashboard, Báo cáo (read-only)
Role: checkin   → Check-in (chỉ page G1)
```

---

## Related Code Files

### Tạo mới

```
src/Mms.Web/
├── Program.cs                          # thêm MudBlazor, Auth services
├── App.razor                           # Router + AuthorizeRouteView
├── _Imports.razor                      # global using MudBlazor, etc.
├── wwwroot/
│   ├── app.css                         # custom overrides
│   └── favicon.ico
├── Shared/
│   ├── MainLayout.razor                # shell: sidebar + body
│   ├── MainLayout.razor.css
│   ├── NavMenu.razor                   # role-based nav items
│   ├── NavMenu.razor.css
│   ├── AppTheme.cs                     # MudTheme custom (palette, typography)
│   ├── NotFound.razor                  # 404 page
│   └── Error.razor                     # 500 page (ẩn stack trace)
├── Pages/
│   ├── Auth/
│   │   ├── Login.razor                 # A1
│   │   ├── ForgotPassword.razor        # A2 stub
│   │   └── ChangePassword.razor        # A3
│   └── Dashboard/
│       └── DashboardPage.razor         # B1 (mock data)
└── Services/
    └── AuthStateProvider.cs            # custom AuthenticationStateProvider

.github/
└── workflows/
    └── ci-build-test.yml               # build + unit test + docker build smoke
```

---

## Implementation Steps

### Bước 1: MudBlazor Setup

1. Cài NuGet: `MudBlazor` v6.x vào `Mms.Web`.
2. `Program.cs`: thêm `builder.Services.AddMudServices()`.
3. `_Imports.razor`: thêm `@using MudBlazor`.
4. `App.razor` (hoặc `_Host.cshtml`): thêm MudBlazor CSS + JS:
   ```html
   <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet"/>
   <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet"/>
   <!-- cuối body -->
   <script src="_content/MudBlazor/MudBlazor.min.js"></script>
   ```
   **LAN offline**: download fonts và host locally (không phụ thuộc Google CDN).

5. Tạo `AppTheme.cs` — MudTheme custom:
   - Primary: `#1a3a5c` (navy)
   - Secondary: `#c9a84c` (gold)
   - Typography: Roboto 14px base
   - AppBar height: 64px

### Bước 2: App.razor + Auth Shell

1. `App.razor`:
   ```razor
   <CascadingAuthenticationState>
       <Router AppAssembly="@typeof(App).Assembly">
           <Found Context="routeData">
               <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                   <NotAuthorized>
                       @if (!context.User.Identity!.IsAuthenticated)
                       { <RedirectToLogin /> }
                       else
                       { <MudText>Không có quyền truy cập.</MudText> }
                   </NotAuthorized>
               </AuthorizeRouteView>
           </Found>
           <NotFound><NotFound /></NotFound>
       </Router>
   </CascadingAuthenticationState>
   ```
2. Tạo component `RedirectToLogin.razor` (dùng `NavigationManager.NavigateTo("/login")`).

### Bước 3: MainLayout + NavMenu

1. `MainLayout.razor`: dùng `MudLayout` + `MudAppBar` + `MudDrawer` + `MudMainContent`.
   - AppBar: logo, tên hệ thống, user dropdown (đổi mật khẩu / logout).
   - Drawer: NavMenu sidebar.
2. `NavMenu.razor`: dùng `<AuthorizeView Roles="admin,operator">` để bọc từng nhóm menu.
   - Dùng `MudNavMenu` + `MudNavLink` + `MudNavGroup`.
   - Icon từ MudBlazor Icons (built-in).

### Bước 4: Auth Pages

**A1 — Login.razor**:
```razor
@page "/login"
@layout EmptyLayout   ← layout không có sidebar (chỉ centered card)

<EditForm Model="@loginModel" OnValidSubmit="HandleLogin">
    <DataAnnotationsValidator/>
    <MudCard>
        <MudCardContent>
            <MudText Typo="Typo.h5">HỆ THỐNG MMS</MudText>
            <MudTextField @bind-Value="loginModel.Username" Label="Tên đăng nhập" />
            <MudTextField @bind-Value="loginModel.Password" Label="Mật khẩu"
                          InputType="InputType.Password" />
        </MudCardContent>
        <MudCardActions>
            <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled"
                       Color="Color.Primary" FullWidth="true">ĐĂNG NHẬP</MudButton>
        </MudCardActions>
    </MudCard>
</EditForm>
```
- Gọi `POST /api/auth/login` → lưu JWT vào localStorage (hoặc memory state).
- Nếu `must_change_password` → `NavigationManager.NavigateTo("/change-password")`.
- Sai → `Snackbar.Add("Tên đăng nhập hoặc mật khẩu không đúng", Severity.Error)`.
- Tạo `EmptyLayout.razor` (chỉ `@Body`, không có sidebar).

**A2 — ForgotPassword.razor** (stub):
- Form email → submit → toast "Nếu email tồn tại, link phục hồi đã được gửi." (không gọi API thật).
- TODO: implement SMTP khi có Phase email.

**A3 — ChangePassword.razor** (`[Authorize]`):
- 3 fields: mật khẩu cũ, mới, xác nhận.
- Validate: mới ≠ cũ, xác nhận = mới, đủ policy.
- Gọi `PUT /api/auth/change-password` → success → `MustChangePassword=false` → toast + redirect `/`.

### Bước 5: Dashboard B1 (skeleton)

1. `DashboardPage.razor` (`@page "/"`, `[Authorize]`):
   - 3 MudCard sections: Cuộc họp (link → /meetings), Thống kê (mock numbers), Cấu hình (links).
   - Bảng "Cuộc họp gần nhất": `MudDataGrid` với mock 1-2 rows hardcode.
   - Bảng "Lịch sử cuộc họp": mock data.
   - TODO comment: "// TODO Phase-03: inject IMeetingService để load data thật".
2. Chỉ cần UI đúng layout theo spec — data thật ở phase-03.

### Bước 6: Error Pages

1. `NotFound.razor`: friendly 404 với link về Dashboard.
2. `Error.razor`: friendly 500, ẩn stack trace khi `!env.IsDevelopment()`.
3. `Program.cs`: cấu hình `app.UseExceptionHandler("/error")`.

### Bước 7: AuthenticationStateProvider (nếu cần custom)

Nếu dùng Cookie Auth (không JWT trong browser):
1. Blazor Server dùng Cookie scheme — `SignInManager.PasswordSignInAsync` + `HttpContext.SignInAsync`.
2. Không cần custom `AuthenticationStateProvider` — dùng default của ASP.NET Core Cookie + Blazor Server built-in.
3. Lưu ý: `HttpContext` trong Blazor Server chỉ available qua `IHttpContextAccessor` — inject cẩn thận (chỉ dùng ở `AuthController`, không inject vào component).

### Bước 8: CI/CD GitHub Actions

Tạo `.github/workflows/ci-build-test.yml`:
```yaml
name: CI — Build & Test
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release \
               --filter "Category!=E2E" \
               --logger "trx;LogFileName=test-results.xml"
      - uses: dorny/test-reporter@v1
        if: always()
        with: { name: Test Results, path: '**/*.xml', reporter: dotnet-trx }

  docker-smoke:
    runs-on: ubuntu-latest
    needs: build-and-test
    steps:
      - uses: actions/checkout@v4
      - run: docker build -f docker/blazor-app.Dockerfile -t mms-app:smoke .
```

---

## Todo List

- [ ] Cài MudBlazor NuGet + setup _Imports.razor
- [ ] Tạo AppTheme.cs (custom palette navy/gold)
- [ ] Tạo App.razor với CascadingAuthenticationState + AuthorizeRouteView
- [ ] Tạo EmptyLayout.razor (cho login page)
- [ ] Tạo MainLayout.razor (AppBar + Drawer)
- [ ] Tạo NavMenu.razor với role-based visibility (4 roles)
- [ ] Tạo RedirectToLogin.razor component
- [ ] Tạo Login.razor (A1) — form + validation + error toast
- [ ] Test login happy path + wrong password
- [ ] Test MustChangePassword redirect
- [ ] Tạo ForgotPassword.razor (A2) — stub
- [ ] Tạo ChangePassword.razor (A3) — full implementation
- [ ] Tạo DashboardPage.razor (B1) — mock data, đúng layout
- [ ] Tạo NotFound.razor + Error.razor
- [ ] Cấu hình UseExceptionHandler
- [ ] Tạo ci-build-test.yml
- [ ] Verify CI pass trên GitHub
- [ ] Download Roboto font + host locally (offline LAN)

---

## Success Criteria

- [ ] Truy cập `http://localhost:8080` → redirect `/login`.
- [ ] Login `admin/Admin@2026!` → redirect `/change-password` (MustChangePassword=true).
- [ ] Sau đổi mật khẩu → redirect `/` (Dashboard).
- [ ] Logout → redirect `/login`, không thể back browser vào Dashboard.
- [ ] Role `checkin` login → chỉ thấy menu Check-in trong NavMenu.
- [ ] GitHub Actions CI xanh trên push.
- [ ] Docker image build thành công trong CI.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| HttpContext không available trong Blazor component | High | Chỉ dùng HttpContext ở AuthController (non-Blazor); dùng Blazor auth state trong component |
| Google Fonts CDN không load (air-gap LAN) | Medium | Self-host Roboto font trong `wwwroot/fonts/` |
| MudBlazor version conflict với .NET 8 | Low | Pin version MudBlazor 6.x, kiểm tra compatibility matrix |
| Blazor Server circuit disconnect khi idle | Low | Cấu hình `CircuitOptions.DisconnectedCircuitRetentionPeriod = 3min` |

---

## Security Considerations

- Login page: không tiết lộ "username không tồn tại" vs "sai mật khẩu" — luôn trả lỗi chung.
- Error page: ẩn exception detail (`env.IsProduction()`).
- Anti-CSRF: Blazor Server tự xử lý qua SignalR connection — không cần thêm.
- Headers security: thêm `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff` vào middleware.
- Rate limiting login: `dotnet` built-in `RateLimiter` — 5 attempts/1min/IP.

---

## Next Steps

- Phase-03 dùng `MainLayout` shell này để thêm pages Company + Meeting.
- Phase-03 cần: inject `IMeetingService` vào `DashboardPage.razor` thay mock data.
- Phase-04 cần: thêm menu item "Import VSDC" vào NavMenu (đã có placeholder).
- Phase-05 (E2E): Playwright test từ trang Login — base URL `http://localhost:8080`.

---

## Unresolved Questions

1. **Offline fonts**: xác nhận LAN có thể truy cập Google Fonts không? Nếu không → bắt buộc self-host.
2. **Blazor Cookie vs JWT trên browser**: team chọn cookie auth (đơn giản hơn) hay localStorage JWT? Cookie an toàn hơn nhưng cần HTTPS. Khuyến nghị: Cookie cho Blazor Server.
3. **Theme màu**: navy/gold là gợi ý. Product/Designer cần confirm color palette chính thức.
