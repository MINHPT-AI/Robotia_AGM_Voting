# KNOWLEDGE ARTIFACT: Context & Style Notes
// SUMMARY: Tài liệu này chứa bối cảnh, logic và chuẩn code cho các Phase từ 0 đến 6B (bao gồm User Account Management, Interactive Islands Pattern) của dự án MMS. Giúp AI nắm bắt dự án nhanh gọn (dưới 15 giây/phase).

---

## // SUMMARY: Phase 00 – Khởi tạo Clean Architecture & Docker, C#, .NET 8, chú ý thiết lập môi trường chéo Windows/Linux.

### Mục đích của Phase 00:
- **Chức năng chính:** Tạo khung sườn project tĩnh và orchestration container.
- **Kết quả mong muốn:** Solution với 6 layer (Domain, App, Infra, Web, PrintAgent, Tests) và docker-compose chạy mượt mà.

### Công nghệ & cấu trúc:
- **Ngôn ngữ / Framework:** C# 12, .NET 8 SDK, Docker.
- **Vị trí trong flow:** Nền tảng tĩnh, thực hiện trước khi code bất kỳ logic nào.

### Cách làm (logic chính):
- **Bước 1:** Dùng `dotnet new` chia layer.
- **Bước 2:** Cấu trúc `Directory.Build.props` ép phiên bản/cảnh báo Nullable chung toàn solution.
- **Bước 3:** Tạo multi-stage `Dockerfile` tối ưu layer caching và `docker-compose.yml` map các volume.

### Phong cách & giọng AI:
- **Ngôn ngữ & tên:** Đặt tên tiếng Anh chuẩn .NET/Docker.
- **Độ phức tạp:** Tập trung tối ưu infra nhẹ nhất.
- **Lưu ý:** Không lặp lại package version ở từng project.

### Điểm review quan trọng:
- **Cần AI sau xem kỹ:** Môi trường Build context trong `.dockerignore`, Map Volumes, thiết lập `--no-restore` khi build container.

---

## // SUMMARY: Phase 01 – Database Entity Framework Core & Identity auth, C#, PostgreSQL, chú ý Optimistic Concurrency và Audit Logging.

### Mục đích của Phase 01:
- **Chức năng chính:** Thiết lập schema dữ liệu và kiến trúc bảo mật Identity.
- **Kết quả mong muốn:** Code-first DB Migration thành công, Seed user khởi tạo, có bảng `audit_logs` chống chỉnh sửa.

### Công nghệ & cấu trúc:
- **Ngôn ngữ / Framework:** C#, EF Core 8, Npgsql PostgreSQL, ASP.NET Core Identity.
- **Vị trí trong flow:** Infrastructure Layer, tiền đề thao tác với dữ liệu.

### Cách làm (logic chính):
- **Bước 1:** Tạo Entities (Mms.Domain) và cấu hình DataAnnotations/Fluent API.
- **Bước 2:** Thiết lập AppDbContext, Identity override hashing qua Bcrypt (WorkFactor 12).
- **Bước 3:** Map trigger SQL thô (Raw SQL) vào Migration chặn sửa/xóa bảng AuditLog.
- **Bước 4:** Testcontainers spin-up PostgreSQL riêng để Intergration Test tự động.

### Phong cách & giọng AI:
- **Ngôn ngữ:** Code/Enums tiếng Anh tuyệt đối. Comments giải thích rõ mục đích override.
- **Độ phức tạp:** Code rành mạch theo Repository Pattern / DbContext DI chuẩn hóa.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:** Cột Xmin (Postgres RowVersion) chống ghi đè concurrency, trigger SQL lỗi dialect báo theo từng DB Engine.
- **Cần AI sau xem kỹ:** Thiết lập Ignore `PendingModelChangesWarning` từ EF Core 8.

---

## // SUMMARY: Phase 02 – Giao diện Auth & Dashboard với Blazor Server, Static SSR bypass Cookie, chú ý Anti-forgery flow.

### Mục đích của Phase 02:
- **Chức năng chính:** UI Shell nền tảng, thiết lập phân quyền (Roles) và chức năng Đăng nhập.
- **Kết quả mong muốn:** Đăng nhập an toàn không rớt session, giao diện Dashboard render chuẩn tĩnh động kết hợp.

### Công nghệ & cấu trúc:
- **Ngôn ngữ / Framework:** C#, Blazor Server SSR (.NET 8), MudBlazor v9.
- **Vị trí trong flow:** Web Layer (Front-end), rào chắn cổng vào hệ thống.

### Cách làm (logic chính):
- **Bước 1:** Dùng MudBlazor layout và thiết lập Theme với CssVars.
- **Bước 2:** View Login KHÔNG dùng `@rendermode` (giữ dạng Static SSR), ép Cookie Auth xuống trình duyệt HTTP Request tiêu chuẩn (chặn SignalR websocket conflict).
- **Bước 3:** Set form Login với thuộc tính `data-enhance="false"`.

### Phong cách & giọng AI:
- **Độ phức tạp:** Ưu tiên dễ dọc, tách UX Components vào thư mục chia rõ vùng logic và Presentation.
- **Comment:** Note cảnh báo việc bypass Interactive Server tại dòng code Login.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:** Mất mã khoá sau khi restart Docker container gây lỗi Antiforgery 400. Nhớ Map Volume `dp_keys`.
- **Cần AI sau xem kỹ:** Middleware chặn AntiForgery, vòng lặp xác thực Blazor Router, Binding params Mud TextField trong chế độ Static SSR mất attributes `name`.

---

## // SUMMARY: Phase 03 – Company & Meeting CRUD bằng CQRS/MediatR, FluentValidation, chú ý UI bind form lồng và Storage volume.

### Mục đích của Phase 03:
- **Chức năng chính:** Quản lý thông tin công ty và tạo cấu trúc sự kiện ĐHCĐ, quản lý ứng viên.
- **Kết quả mong muốn:** Dữ liệu chuẩn chỉnh (CQRS pipeline validation), upload persist images qua Volume.

### Công nghệ & cấu trúc:
- **Ngôn ngữ / Framework:** C#, Blazor InteractiveServer, MediatR, FluentValidation.
- **Vị trí trong flow:** Giao diện tính năng nghiệp vụ chính xuất hiện sau Đăng nhập.

### Cách làm (logic chính):
- **Bước 1:** Blazor UI Forms gọi các xử lý bằng `ISender.Send(Command)` (MediatR).
- **Bước 2:** `ValidationBehavior` Pipeline tự động chặn và ném `ValidationException` nếu data bẩn.
- **Bước 3:** Upload File Storage đẩy byte Stream vào thư mục Volume `wwwroot/uploads` mount map lưu trữ ngoại vi host.

### Phong cách & giọng AI:
- **Độ phức tạp:** Logic nghiệp vụ 100% được tập trung trong Command Handlers. UI file Blazor hoàn toàn rất ít dòng code.
- **Comment:** Folder structure phân theo `Feature`.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:** 
  - Lưới lồng (Grid Editing) cập nhật form lặp state dẫn đến Crash DOM.
  - Phân quyền sai khi chown upload folder qua Docker.
  - Lỗi cấu hình `IdentityAlwaysColumn` của Postgres khiến Audit log chèn `Id = 0` và throw Exception (0 rows affected).
  - Lỗi DbUpdateConcurrencyException khi gọi Update/Delete trên Entity có Global Query Filter (`IsDeleted`) lồng Entity con (Change tracker tracking conflict).
- **Cần AI sau xem kỹ:** 
  - Cách Catch exception từ MediatR sang UI Blazor Dialog. 
  - Mount host cho volume ảnh.
  - Update meeting sử dụng kĩ thuật `IgnoreQueryFilters()` và `ExecuteSqlInterpolated` (Raw SQL) để an toàn vượt tầng Audit/Change Tracker.

---

## // SUMMARY: Phase 04 – VSDC Excel Parser & Import Wizard, C#, ClosedXML, chú ý memory mapping, performance bulk.

### Mục đích của Phase 04:
- **Chức năng chính:** Parse file VSDC (Template cứng 16 cột) nhập thông tin DS Cổ đông với Wizard 4 bước.
- **Kết quả mong muốn:** Parse 10,000 dòng/giây không ăn mòn RAM, rollback fail toàn bộ nếu sai format.

### Công nghệ & cấu trúc:
- **Ngôn ngữ / Framework:** C#, ClosedXML 0.104.2, raw Npgsql SQL với `unnest()` arrays.
- **Vị trí trong flow:** Khởi tạo dữ liệu cổ đông trước khi checkin. Route: `/meetings/{id}/import`.

### Cách làm (logic chính):
- **Bước 1:** Upload `.xlsx` → validate extension + size (20MB max) → `VsdcParser.Parse(stream)`.
- **Bước 2:** Parser 3 giai đoạn:
  - `FindHeaderRow()` — Quét 50 dòng đầu tìm cell "STT" (check 5 cột để handle merge).
  - `BuildColumnMap()` — Đọc "dòng số cột" (headerRow+2), tìm cell "1".."16" → build `columnMap[1..16] = physical column index`. **Dynamic, không hardcode.**
  - `ExtractDataRows()` — Iterate data rows, track section (I/II) + sub-section (Cá nhân/Tổ chức), skip 6 loại non-data rows, chỉ lấy rows có FullName + IdNumber.
- **Bước 3:** `VsdcRowMapper.Map()` — Convert raw text → DTO. `ParseVsdcNumber()` xóa dấu chấm hàng nghìn. `ParseVsdcDate()` xử lý cả dd/MM/yyyy và OADate.
- **Bước 4:** `VsdcValidator.Validate()` — 4 Error rules (missing ID/name, zero votes, intra-file duplicate) + 2 Warning rules (DB duplicate, exceeds charter).
- **Bước 5:** `ImportShareholdersHandler` — **DELETE + INSERT** (Wipe-and-Reload): Xóa sạch CĐ cũ của meeting rồi `AddRangeAsync` danh sách mới, batch 500. Target <2s cho 1000 rows. *(Ban đầu dùng `ON CONFLICT DO UPDATE` nhưng đã đổi sang Wipe-and-Reload ở Phase 05 UX Polish do Unique Index bị gỡ.)*

### Phong cách & giọng AI:
- **Độ phức tạp:** Parser code dài để cover merged cells + section tracking chính xác. Handler dùng raw SQL thay EF vì performance.
- **Comment:** Giải thích rõ tại sao check 3 cột khi detect section, tại sao reset subSection khi chuyển section.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:**
  - Merged cells → column mapping SAI nếu không dùng "number row" dynamic detection.
  - Section header text có thể ở bất kỳ cột nào (B, C, D) → phải check nhiều cột.
  - Không reset `currentSubSection` khi section change → tag sai Foreign/Domestic.
  - Số VN dùng dấu chấm hàng nghìn ("18.600" = 18600) — nếu quên `Replace(".", "")` → parse sai thành 18.
  - `NpgsqlParameter` cho nullable arrays (DateOnly?, string?) phải dùng `DBNull.Value`, không dùng `null`.
- **Cần AI sau xem kỹ:** Table/column names trong raw SQL phải khớp EF naming convention (PascalCase quoted). Index unique `(MeetingId, IdNumber)` BẮT BUỘC cho `ON CONFLICT`.

---

## // SUMMARY: Phase 05 (UX Polish) – Hoàn thiện luồng Import VSDC & UX/UI DataGrid, giảm ràng buộc dữ liệu, fix Razor parser bugs.

### Mục đích:
- **Chức năng chính:** Tối ưu hóa UI/UX DataGrid hiển thị hàng nghìn dòng. Thay thế cơ chế Upsert bằng Wipe-and-Reload. Thêm Export CSV client-side.
- **Kết quả mong muốn:** Grid mượt mà, sticky header, sidebar mini-mode, export CSV.

### Cách làm:
- Gỡ Unique Index `(MeetingId, IdNumber)`, dùng `ExecuteSqlRawAsync("DELETE...")` + `AddRangeAsync`.
- Custom CSS `overflow: auto` + `th { position: sticky; top: 0; z-index: 2;}` cho MudDataGrid.
- CSV Export: BOM `\uFEFF` + `JS.InvokeVoidAsync("downloadFileFromBytes", ...)`.
- Drawer dùng `DrawerVariant.Mini` + `OpenMiniOnHover` trong MudBlazor V9.

### Điểm review:
- **Razor Parser Crash:** `{Count:N0}` trong Razor → CS0201. Bắt buộc `.ToString("N0")` trước khi interpolate.
- **CSV Escape:** Replace dồn `""""""` gây treo AST → luôn tách var xử lý trước.

---

## // SUMMARY: Phase 05 (Testing) – Quality Gate PASSED: 39 Unit + 11 Integration + 4 E2E scaffold, Performance ~4s/1000 rows, CI 3-job pipeline.

### Mục đích của Phase 05 (Testing):
- **Chức năng chính:** Test coverage toàn bộ code Phase 01→05, performance benchmark (1,000 CĐ < 10s), CI pipeline, demo docs.
- **Kết quả thực tế:** **50 tests green** (39 Unit + 11 Integration), import 1000 CĐ **~4s**, E2E 4 scenarios scaffolded build clean.

### Công nghệ & cấu trúc:
- **Testing:** xUnit, FluentAssertions, Moq, ClosedXML (in-memory xlsx), Testcontainers.PostgreSql, Microsoft.Playwright.
- **Vị trí:** `tests/Mms.UnitTests`, `tests/Mms.IntegrationTests`, `tests/Mms.E2ETests`.

### Cách làm (logic chính — KẾT QUẢ THỰC TẾ):

- **Unit Tests (39/39 passed, ~3s):**
  - `VsdcParserTests` (8): Header detection, column mapping, section identification, format errors.
  - `VsdcValidatorTests` (6): **Tất cả 6 rules đều trả Warnings** (không phải Errors).
  - `VsdcRowMapperTests` (4+6 Theory): Date/number parsing, Vietnamese thousands separators, OADate.
  - `CreateMeetingValidatorTests` (5) + `UpsertCompanyValidatorTests` (6): FluentValidation rules.
  - Helper `VsdcXlsxBuilder`: Tạo file Excel in-memory mô phỏng VSDC, shared giữa Unit & Integration via `<Compile Link>`.

- **Integration Tests (11/11 passed, ~18s) — Testcontainers PostgreSQL 16:**
  - `DatabaseFixture` upgraded: Đăng ký đầy đủ **MediatR + FluentValidation + ValidationBehaviour** (mirror Program.cs DI). Thêm `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`.
  - `Phase01Tests` (3): Migration, Seed Data, JWT Token (từ Phase 01).
  - `MeetingCrudIntegrationTests` (5): Create with resolutions/candidates, AuditLog `Detail` verify, soft-delete, delete-with-shareholders exception, clone meeting.
  - `ImportFlowIntegrationTests` (3): **Performance gate 1,000 rows ~4s** (target < 10s), Wipe-and-Reload verify (import 2 lần → vẫn đúng count), FK violation rollback (0 leaked rows).

- **E2E Playwright (4 scenarios, build clean — chạy khi docker-compose up):**
  - `PlaywrightFixture` targeting **docker-compose stack** (NOT WebApplicationFactory) — health-check retry loop 60s.
  - Page Objects: `LoginPage`, `DashboardPage`.
  - `LoginScenarioTests` (2): Happy path redirect, wrong password error display.
  - `MeetingAndImportScenarioTests` (2): Meeting form navigation, meetings page load.
  - Chạy: `MMS_E2E_URL=http://localhost:8080 dotnet test tests/Mms.E2ETests/`

- **CI Pipeline (3-job GitHub Actions — `.github/workflows/ci-build-test.yml`):**
  - Job 1: Build + Unit Tests + Integration Tests (auto on push/PR to main, develop).
  - Job 2: Docker Build Smoke Test (needs Job 1).
  - Job 3: E2E Playwright (manual trigger `workflow_dispatch` only).

- **Demo Docs:**
  - `docs/quick-start-guide.md` — Setup, Docker, testing, architecture overview, test pyramid table.
  - `docs/pilot-demo-checklist.md` — Step-by-step demo script 5 sections, performance benchmark table.

### Phong cách & giọng AI:
- **Đặt tên test:** `MethodUnderTest_Scenario_ExpectedResult`.
- **Assertions:** FluentAssertions (`Should().Be...`), KHÔNG Assert.Equal.
- **Test data:** TUYỆT ĐỐI không dùng data thật. Synthetic data only.
- **Performance test:** `Stopwatch` + `.Should().BeLessThan(TimeSpan.FromSeconds(10))`, ghi output via `ITestOutputHelper`.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:**
  - `DatabaseFixture` **PHẢI** có `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` — thiếu sẽ crash CloneMeeting test.
  - AuditLog entity dùng property `Detail` (không phải `Action`) — đọc kỹ Entity trước khi assert.
  - Import strategy là DELETE + INSERT (Wipe-and-Reload) → test `ImportSameFileTwice` phải assert `count == N` (không phải `2*N`).
  - VsdcValidator trả 100% **warnings** — test phải check `result.Warnings`.
  - E2E **KHÔNG** dùng `WebApplicationFactory` (Blazor Server + SignalR không compatible) → test chạy trên docker-compose stack thật.
  - Playwright: dùng `WaitForSelectorAsync()` / `WaitForURLAsync()`, KHÔNG dùng `Task.Delay`.
- **Cần AI sau xem kỹ:**
  - `VsdcXlsxBuilder` shared via `<Compile Include="..." Link="...">` trong csproj — nếu thêm helper mới, phải update cả hai.
  - `CreateFreshDbContext()` helper trong Fixture — dùng khi cần read-after-write verification (tránh EF cache).
  - CI runner cần Docker cho Integration tests (Testcontainers) và `pwsh playwright.ps1 install chromium` cho E2E.

---

## // SUMMARY: Phase 06A – Gửi Thư Mời Giấy (Physical Invitation Letter Management) – Bao gồm Domain Entity, 4 Infrastructure Services (BarQrCode, LetterDocx, LibreOfficePdf, CpnRowMatcher), 7 MediatR Handlers, API endpoints (DOCX/PDF), và Blazor UI 3-tab (Tạo-Xuất / Theo dõi / Import CPN).

### Mục đích của Phase 06A:
- **Chức năng chính:** Quản lý toàn bộ vòng đời thư mời giấy gửi cổ đông — từ tạo danh sách → xuất DOCX/PDF → dispatch → theo dõi giao hàng → import kết quả bưu điện (CPN).
- **Kết quả mong muốn:** Hệ thống end-to-end cho mailing workflow, build clean, tests green, UI 3-tab functional.

### Architecture Decisions:
- **Domain:** `InvitationLetter` entity với 5 `InvitationStatus` (NotSent, Dispatched, Delivered, Failed, Returned) + `CodeMarkType` (Barcode, QRCode, None).
- **Infrastructure Services:**
  - `BarQrCodeGenerator` — ZXing.Net (Code128 barcode) + QRCoder (QR) → PNG byte[]. Singleton DI.
  - `LetterDocxBuilder` — DocumentFormat.OpenXml, A4 C-fold layout (first 99mm content zone), `AltChunk` merge. Transient DI.
  - `LibreOfficePdfConverter` — headless LibreOffice process, 60s timeout, CancellationToken + entireProcessTree kill. Transient DI.
  - `CpnRowMatcher` — 5-tier matching (TrackingCode → Name → Phone → Name+Phone → Address Jaccard). Pre-computed `Dictionary<>` lookups. Vietnamese diacritics normalization (`NormVN`). Transient DI.
- **Application Layer:** 5 Commands + 2 Queries = 7 MediatR handlers. DTOs in `Mms.Application.InvitationLetters.Dtos`.
- **Web Layer:**
  - `LettersController` — REST endpoints `GET /api/meetings/{id}/letters/export/docx` và `/pdf`.
  - `InvitationLettersPage.razor` — 3-tab: (1) Tạo & Xuất, (2) Theo dõi DataGrid + inline status dialog, (3) CPN Import Wizard 3 bước (Upload → Preview Match → Confirm).

### NuGet Packages (thêm Phase 06A):
- `QRCoder 1.6.0` — QR code generation
- `ZXing.Net 0.16.9` — Barcode generation (Code128)
- `DocumentFormat.OpenXml 3.2.0` — DOCX builder
- `SixLabors.ImageSharp 3.1.5` — Image processing for barcode rendering

### Tên file quan trọng (không đoán):
- `src/Mms.Domain/Entities/InvitationLetter.cs`
- `src/Mms.Application/Interfaces/ILetterServices.cs`
- `src/Mms.Application/InvitationLetters/Commands/LetterCommands.cs`
- `src/Mms.Application/InvitationLetters/Queries/LetterQueries.cs`
- `src/Mms.Application/InvitationLetters/Dtos/LetterDtos.cs`
- `src/Mms.Infrastructure/Documents/BarQrCodeGenerator.cs`
- `src/Mms.Infrastructure/Documents/LetterDocxBuilder.cs`
- `src/Mms.Infrastructure/Documents/LibreOfficePdfConverter.cs`
- `src/Mms.Infrastructure/Parsing/CpnRowMatcher.cs`
- `src/Mms.Infrastructure/Handlers/InvitationLetters/LetterHandlers.cs`
- `src/Mms.Infrastructure/Persistence/Configurations/InvitationLetterConfiguration.cs`
- `src/Mms.Web/Api/LettersController.cs`
- `src/Mms.Web/Components/Pages/Meetings/InvitationLettersPage.razor`

### Phong cách & giọng AI:
- **CpnRowMatcher:** Luôn normalize Unicode NFKD + lowercase + regex `[\p{M}\s]` remove diacritics TRƯỚC khi so sánh.
- **LetterDocxBuilder:** Margin + SectionProperties tính bằng **twips** (1 inch = 1440 twips). C-fold window zone = first 99mm = ~5620 twips.
- **LibreOfficePdfConverter:** PHẢI kill `entireProcessTree: true`, nếu không zombie `soffice.bin` chiếm port.
- **Import CPN:** `DryRun = true` trước khi `DryRun = false` — UI bắt buộc preview trước confirm.

### Điểm review quan trọng (Hotfixes):
- **Chỗ dễ lỗi (Export PDF/DOCX):**
  - LibreOffice chạy Docker user non-root sẽ bị báo Permission Denied do thiếu config profile thư mục ảo. PHẢI thêm lệnh `--env:UserInstallation=file:///tmp/libreoffice` vào lệnh khởi động `soffice` trên Ubuntu container.
  - Container Blazor cần cài LibreOffice từ Package manager: thêm lệnh cài đặt `libreoffice-writer`, `default-jre`, và fonts trong Dockerfile Runtime layer.
  - Mã QR code được sinh ra hình vuông, nhưng Barcode hình chữ nhật. Khi nhúng vào Word qua XML thì PHẢI kiểm tra `CodeMarkType` để tuỳ chỉnh giá trị Extents cho đúng chiều ngang và dọc (EMU) để tránh hình bị kéo dãn bẹp nhúm.
- **Lưu ý UI / Chức năng nâng cao CPN:**
  - Ở màn hình Preview CPN import, cần cung cấp cho người dùng tính năng xem Side-by-side (MudDialog) giữa 2 bản ghi DB và CPN với dữ liệu Address/Phone đầy đủ và công cụ Filter kết quả theo `CpnMatchResult.Confidence` (MudChip) để tăng hiệu quả rà soát dữ liệu thủ công.
  - `JustificationValues.Left` KHÔNG phải compile-time constant → dùng `JustificationValues? alignment = null` + null coalesce.
  - `InvitationLetter.ShareholderIdNumber` là FK logic (không phải EF FK) — dùng string match, KHÔNG navigation property đến Shareholder.
  - `ClosedXML` used in both `ImportShareholdersHandler` (Phase 04) và `ImportCpnReportHandler` (Phase 06A) — package đã có sẵn.

---

// SUMMARY: Phase 06B – Quản lý Tài Khoản & Phân Quyền (User Account Management & Authorization) – ApplicationUser.IsActive, 6 Commands + 2 Queries + 5 Validators, 3 trang mới (User Management, Audit Log, Profile), UserMenu interactive component, Route Authorization audit.

### Mục đích của Phase 06B:
- **Chức năng chính:** Hoàn thiện tầng quản trị người dùng: CRUD tài khoản, gán role, enable/disable, reset password, self-service profile + password change, audit log viewer.
- **Kết quả mong muốn:** Admin quản lý toàn bộ user qua UI, không cần thao tác DB thủ công. Route authorization áp đúng Role Matrix cho mọi trang.

### Domain/Entity Changes:
- `ApplicationUser.IsActive` (bool, default true) — disable = `IsActive=false` + `LockoutEnabled=true` + `LockoutEnd=MaxValue`. Identity tự chặn login.
- EF Migration: `AddUserIsActiveField`.

### Application Layer (`src/Mms.Application/Users/`):
- **Commands (6):** `CreateUserCommand`, `UpdateUserCommand`, `ToggleUserActiveCommand`, `AdminResetPasswordCommand`, `UpdateProfileCommand`, `ChangeOwnPasswordCommand`.
- **Queries (2):** `GetUsersQuery` (paged, with roles), `GetAuditLogsQuery` (paged + filter by date/entity/performer).
- **Validators (5):** `CreateUserValidator`, `UpdateUserValidator`, `AdminResetPasswordValidator`, `UpdateProfileValidator`, `ChangeOwnPasswordValidator`.
- **DTOs:** `UserListItemDto`, `AuditLogDto`.

### Infrastructure Handlers (`src/Mms.Infrastructure/Handlers/Users/UserHandlers.cs`):
- Tất cả handlers inject `UserManager<ApplicationUser>` trực tiếp (không DbContext cho user ops).
- `GetAuditLogsHandler` dùng `MmsDbContext.AuditLogs.AsNoTracking()`.
- Password operations: `ChangePasswordAsync` (self), `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync` (admin reset).

### Web UI:
- `/admin/users` — `UserManagementPage.razor` + 3 dialogs (`CreateUserDialog`, `EditUserDialog`, `ResetPasswordDialog`).
- `/admin/audit-log` — `AuditLogPage.razor` (read-only, date/entity/performer filter).
- `/account/profile` — `ProfilePage.razor` (2 cards: profile edit + password change).
- NavMenu: Added "Hồ sơ của tôi" link visible to all authenticated users.

### Layout — Interactive UserMenu Component:
- **`src/Mms.Web/Components/Layout/UserMenu.razor`** — component riêng với `@rendermode InteractiveServer`.
- Hiển thị Avatar (chữ cái đầu) + Username + MudMenu dropdown (Hồ sơ / Đổi mật khẩu / Đăng xuất).
- **MainLayout gọi `<UserMenu />` bên trong `<AuthorizeView>`** — không chứa inline MudMenu nữa.
- Logic lấy tên user (`AuthenticationStateProvider`) và xử lý logout nằm hoàn toàn trong `UserMenu.razor`.

### Role Matrix (áp dụng toàn dự án):
| Route | admin | operator | checkin | viewer |
|-------|-------|----------|---------|--------|
| `/admin/*` | ✅ | ❌ | ❌ | ❌ |
| `/meetings`, `/meetings/*` | ✅ | ✅ | ❌ | ❌ |
| `/company` | ✅ | ❌ | ❌ | ❌ |
| `/checkin` | ✅ | ✅ | ✅ | ❌ |
| `/reports` | ✅ | ✅ | ❌ | ✅ |
| `/account/profile` | ✅ | ✅ | ✅ | ✅ |

### Phong cách & giọng AI:
- **Dialog pattern:** `MudDialogService.ShowAsync<TComponent>()` cho CRUD, `ShowMessageBoxAsync()` cho confirm toggle.
- **MudBlazor v9:** `ServerData` delegate cần `CancellationToken` parameter. `MudDataGrid` page 0-based.
- **Error handling:** `AppValidationException` bắt từ MediatR pipeline, hiển thị inline `MudAlert` hoặc `Snackbar`.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:**
  - `ShowMessageBoxAsync` (KHÔNG phải `ShowMessageBox`) — MudBlazor v9 renamed.
  - `GridState.Page` is 0-based → phải `+1` khi truyền vào Query.
  - `UserManager.GetRolesAsync()` trả `IList<string>` — dùng `FirstOrDefault()` vì mỗi user chỉ 1 role.
  - Disable user phải set cả 3 fields (`IsActive`, `LockoutEnabled`, `LockoutEnd`) — thiếu 1 field sẽ không chặn login hoàn toàn.
  - **Xung đột `context` (RZ9999):** Khi lồng `MudMenu` (có `ActivatorContent`) bên trong `AuthorizeView`, cả hai dùng implicit `context`. Fix: đặt `Context="menuContext"` trên `ActivatorContent` hoặc tách component riêng.
- **Cần AI sau xem kỹ:**
  - Route authorization audit — tất cả pages đã có đúng `[Authorize]` attribute theo Role Matrix.
  - Profile page dùng `AuthenticationStateProvider` + `UserManager.GetUserAsync()` lấy current user.
  - ChangePassword trong Profile page KHÔNG redirect (khác `ChangePassword.razor` force-change).
  - **⚠️ CRITICAL — Interactive Islands Pattern trong Static Layout:**
    - MainLayout KHÔNG có `@rendermode` → render Static SSR.
    - Mọi component con trong Layout cần JS interactivity (MudMenu, MudDialog...) **PHẢI** được tách ra file riêng có `@rendermode InteractiveServer`.
    - File mẫu: `UserMenu.razor` — tách khỏi MainLayout, tự quản lý AuthState + NavigationManager.
    - Nếu thêm component interactive mới vào Layout (VD: notification bell, theme toggle), **LUÔN** tạo component riêng.

### Tên file quan trọng (không đoán):
- `src/Mms.Web/Components/Layout/UserMenu.razor` ← **Interactive component trong static layout**
- `src/Mms.Web/Components/Layout/MainLayout.razor`
- `src/Mms.Web/Components/Layout/NavMenu.razor`
- `src/Mms.Web/Components/Pages/Admin/UserManagementPage.razor`
- `src/Mms.Web/Components/Pages/Admin/AuditLogPage.razor`
- `src/Mms.Web/Components/Pages/Account/ProfilePage.razor`
- `src/Mms.Application/Users/UserCommands.cs`
- `src/Mms.Application/Users/UserQueries.cs`
- `src/Mms.Application/Users/UserValidators.cs`
- `src/Mms.Infrastructure/Handlers/Users/UserHandlers.cs`

---

## // SUMMARY: Phase 07 – Quản Lý Mẫu Văn Bản (Template Management) – Upload DOCX, Token Scanning, Preview PDF, Finalize/Clone/Delete, Phase 06A Template-aware Letter Export.

### Mục đích của Phase 07:
- **Chức năng chính:** Thư viện mẫu văn bản toàn hệ thống — admin upload file DOCX chứa token placeholder, hệ thống scan token tự động, preview PDF qua LibreOffice, chốt mẫu (lock), clone, xóa. Khi xuất thư mời (Phase 06A) hệ thống tự tìm template finalized để fill data thay vì dùng synthetic.
- **Kết quả mong muốn:** Admin quản lý 6 loại mẫu (Invitation, VotingCard, ElectionBallot, AttendanceReport, CountingReport, Minutes) qua UI, token bắt buộc/tuỳ chọn được scan + cảnh báo, letter export tự động dùng template khi có.

### Domain/Entity Changes:
- `Template.MeetingId` → **nullable** (null = global library template).
- Thêm `Template.Name` (string, 200 ký tự) — tên gợi nhớ do admin đặt.
- Thêm `Template.FileSize` (long?) — bytes.
- EF Migration: `Phase07_TemplateSchemaUpdate`.

### Architecture:
- **TokenRegistry** (`src/Mms.Application/Templates/TokenRegistry.cs`) — Static class định nghĩa token bắt buộc/tuỳ chọn cho 6 TemplateType. Token format: `{{PascalCase}}`.
- **ITemplateFileService** — Interface tại Application layer, implementation tại Infrastructure.
  - `SaveAsync()` → lưu file vào `wwwroot/uploads/templates/{guid}.docx`
  - `ScanTokensAsync()` → OpenXml extract text + Regex `\{\{[A-Za-z]+\}\}` → detect + missing required
  - `ConvertToPdfPreviewAsync()` → delegate to `ILibreOfficePdfConverter`
- **5 Commands + 3 Queries** via MediatR — Upload, UpdateName, Finalize, Clone, Delete, GetList, GetPlaceholders, PreviewPdf.
- **Phase 06A Integration:** `ExportLettersDocxHandler` lookup template: meeting-specific finalized → global finalized → synthetic fallback. `LetterDocxBuilder.BuildFromTemplate()` performs token find-replace trên DOCX upload.

### NuGet Packages:
- Không thêm package mới — tái dùng `DocumentFormat.OpenXml` (đã có Phase 06A).

### Tên file quan trọng (không đoán):
- `src/Mms.Domain/Entities/Template.cs`
- `src/Mms.Application/Templates/TokenRegistry.cs`
- `src/Mms.Application/Templates/TemplateDtos.cs`
- `src/Mms.Application/Templates/TemplateCommands.cs`
- `src/Mms.Application/Templates/TemplateQueries.cs`
- `src/Mms.Application/Templates/TemplateValidators.cs`
- `src/Mms.Application/Interfaces/ITemplateFileService.cs`
- `src/Mms.Infrastructure/Documents/TemplateFileService.cs`
- `src/Mms.Infrastructure/Handlers/Templates/TemplateHandlers.cs`
- `src/Mms.Web/Api/TemplatesController.cs`
- `src/Mms.Web/Components/Pages/Admin/TemplateLibraryPage.razor`
- `src/Mms.Web/Components/Pages/Admin/UploadTemplateDialog.razor`
- `src/Mms.Web/Components/Pages/Admin/CloneTemplateDialog.razor`
- `src/Mms.Web/Components/Pages/Admin/UpdateTemplateNameDialog.razor`

### Phong cách & giọng AI:
- **Token format:** `{{PascalCase}}` — double curly braces, không có dấu cách.
- **Template lookup:** Luôn ưu tiên meeting-specific → global → synthetic fallback.
- **MudSelect bind:** KHÔNG dùng đồng thời `@bind-Value` và `ValueChanged` — dùng `Value` + `ValueChanged` thay thế.
- **Razor < operator:** Switch expression dùng `< 1024` trong `.razor` file sẽ crash Razor parser — thay bằng `if (value < 1024)` khi ở trong `@code` block.

### Điểm review quan trọng:
- **Chỗ dễ lỗi:**
  - Razor parser crash khi dùng `switch { < 1024 => ... }` trong file `.razor` — Razor hiểu `<` là mở tag HTML. **Luôn dùng if-else thay switch với < operator trong Razor.**
  - `MudChip.OnClick` cần prefix `@` khi truyền lambda: `OnClick="@(() => Method())"` không phải `OnClick="() => Method()"`.
  - `MudSelect` KHÔNG dùng đồng thời `@bind-Value` và `ValueChanged` — gây conflict, dùng `Value` + `ValueChanged`.
  - `LetterBuildDto` giờ là record với **init-only properties** cho meeting-level fields — caller phải truyền khi construct.
  - Clone template: file vật lý được copy (new GUID), Version tăng 1, `IsFinalized = false`, `MeetingId = null`.
  - Finalize guard: template PHẢI có `FilePath` mới được chốt.
  - Delete guard: template đã `IsFinalized = true` KHÔNG được xóa.
- **Cần AI sau xem kỹ:**
  - Token scan dùng `string.Concat(body.Descendants<Text>())` — nếu token bị split qua nhiều Run (do Word formatting) thì scan sẽ miss. Giải pháp: admin cần paste token dạng plain text.
  - `BookmarkStart` named `BARCODE_MARK` trong template DOCX cho phép insert barcode/QR tại vị trí cụ thể.
  - Template preview PDF chạy qua LibreOffice headless — chỉ hoạt động trong Docker container.

---

## // SUMMARY: Phase 07 v2.2 – Nâng cấp WYSIWYG Template Editor & Live Preview – Tùy chỉnh Margin, định dạng Paragraph chuẩn NĐ 30, Chèn con dấu đè lên chữ ký tuyệt đối (absolute CSS), lỗi TinyMCE 7 plugins.

### Mục đích của Phase 07 v2.2:
- **Chức năng chính:** Trình soạn thảo văn bản hành chính NĐ 30/2020 trên web với thanh công cụ TinyMCE 7, lưu Margin xuống DB (`Template` entity), và preview Live HTML sang PDF với dữ liệu thực tế.

### Điểm review quan trọng (Lỗi đã gặp):
- **⚠️ CRITICAL BUG — TinyMCE 7 Initialization Crash (Textarea hiển thị mã HTML raw):**
  - *Mô tả:* Khi tích hợp cấu hình `plugins` của TinyMCE, các plugin không còn tồn tại trong bản v7 như `lineheight`, `hr`, `print` sẽ gây crash ngầm Javascript, làm trình soạn thảo không load được, trả về UI khung `<textarea>` thô.
  - *Lỗi dây chuyền:* Khi TinyMCE crash, việc gọi biến `tinymce.get('mce-editor')` để lấy nội dung HTML trả về Null hoặc Exception, gây tê liệt chức năng của nút "Xem trước" (Preview) khi gọi thông qua JS Interop từ Blazor.
  - *Cách giải quyết đã áp dụng:* 
    1. Lọc bỏ các plugin hỏng khỏi string cấu hình `plugins` trong file `wwwroot/js/template-editor.js`.
    2. Bọc try-catch hoặc fallback cho hàm `getContent` (sử dụng thẳng value của thẻ `textarea` nếu `tinymce.get` fail).
    3. Luôn phải **Hard Refresh** trình duyệt sau khi deploy để xóa cache JS cũ trên máy người dùng.

### Tên file quan trọng được nâng cấp:
- `src/Mms.Web/wwwroot/js/template-editor.js` ← **Cấu hình TinyMCE + JS Interop**
- `src/Mms.Web/Components/Pages/Admin/TemplateEditorPage.razor`
- `src/Mms.Web/Components/Pages/Admin/PreviewDialog.razor`
- `src/Mms.Infrastructure/Handlers/Templates/TemplateHandlers.cs`

---

## // SUMMARY: Phase 08 – Quản Lý Ủy Quyền (Proxy Management) – Domain extensions cho Proxy/Checkin/Tally, 5 commands/queries, UI 2-cột.

### Mục đích của Phase 08:
- **Chức năng chính:** Mở rộng Data Model cho toàn bộ luồng họp (Ủy quyền, Check-in, Bỏ phiếu) và hoàn thiện chức năng Quản lý Ủy quyền (Thêm/Hủy ủy quyền, in giấy ủy quyền).
- **Kết quả mong muốn:** Người dùng (cổ đông) có thể ủy quyền cho người khác (VSDC hoặc khách mời ngoài), có validation chặt chẽ số lượng cổ phần.

### Domain/Entity Changes:
- Bổ sung 8 entities mới: `ProxyRecipient`, `MeetingTemplateConfig`, `AttendanceRecord`, `BallotGroup`, `AttendanceSnapshot`, `VoteResult`, `ElectionVote`, `TallySnapshot`.
- Bổ sung/cập nhật 12 Enums.
- EF Core Constraints: RB-04 (1 attendance/CĐ), RB-11 (unique source shareholder per ballot). Chống tranh chấp dữ liệu (Optimistic Concurrency) trên bảng `Ballot`.

### Architecture & UI:
- **Application Layer:** Các lệnh `CreateProxyCommand`, `CancelProxyCommand`, và các Queries tìm kiếm proxy/shareholders.
- **Web Layer:** `ProxyManagementPage.razor` thiết kế 2 cột: form tìm kiếm/nhập liệu bên trái, danh sách nhận ủy quyền bên phải dạng Drawer.
- **Business Rules:** Tự động tạo `ProxyRecipient` nếu người nhận không có trong danh sách VSDC. Khóa chức năng ủy quyền khi Đại hội đã Check-in hoặc Kiểm phiếu.

### Điểm review quan trọng:
- EF Core sử dụng `Property(b => b.Xmin).IsRowVersion()` để xử lý tranh chấp cấp database thay vì token sinh tay.

---

## // SUMMARY: Phase 09 – Bàn Check-in & Thẩm Tra Tư Cách (Check-in Workbench & Attendance Qualification) – Atomic Check-in Transactions, SignalR Realtime Sync, UI Status Banner.

### Mục đích của Phase 09:
- **Chức năng chính:** Cho phép nhân viên check-in cổ đông, in phiếu biểu quyết, gộp tài khoản trùng CCCD, và tính toán Quorum (Tỷ lệ tham dự) chốt danh sách mở đại hội.
- **Kết quả mong muốn:** Transaction check-in tuyệt đối an toàn (không sinh phiếu nửa vời), giao diện cập nhật realtime giữa các quầy qua SignalR, tính Quorum server-side chính xác.

### Architecture Decisions:
- **Atomic Transaction:** Handler `PerformCheckinHandler` bọc toàn bộ logic tạo `AttendanceRecord` + sinh 4 loại phiếu (`Ballot`) vào chung một DbContext Transaction. Rollback toàn bộ nếu lỗi.
- **SignalR Sync:** `CheckinHub` được sử dụng để phát sự kiện (broadcast) cập nhật số liệu Topbar (Tổng tham dự, Tổng cổ phần) tới mọi màn hình POS theo thời gian thực mà không cần F5.
- **Situation Handling (F1-F4):** Phân loại các tình huống (CĐ tự đi, CĐ ủy quyền toàn bộ, CĐ nhận ủy quyền...) bằng `SituationCode` để UI `CheckinWorkbenchPage.razor` render Form và Banner tương ứng một cách linh hoạt.

### Điểm review quan trọng (Hotfixes Phase 08 & 09):
- **Chỗ dễ lỗi (Quorum Sync & DS1):** Danh sách DS1 phải là **Single Source of Truth** cho Topbar Quorum. Không tính toán Topbar độc lập. Hàm `GetAttendanceListHandler` phải filter những shareholder có `DirectShares == 0 && ProxyShares == 0` (F0) ra khỏi danh sách. Tổng số CP từ DS1 sẽ được `GetCheckinTopbarHandler` cộng lại.
- **Bulk Proxy Import (Memory Validation):** Khi import file Excel ủy quyền, cần dùng cấu trúc `Dictionary` (vd `usedSharesDict`) để cộng dồn proxy shares trên RAM và so sánh liên tục với `VotingRights` nhằm ngăn chặn vượt quyền hạn trong cùng 1 file import.
- **Just-In-Time (JIT) Migration cho Người ủy quyền bên ngoài:** 
  - *Vấn đề:* Khách mời bên ngoài nhận ủy quyền được lưu ở `ProxyRecipients`, nhưng Check-in lại bắt buộc cần `ShareholderId` (VSDC). Nếu import Excel nhiều lần sẽ gây rác dữ liệu trùng lặp CCCD ở `ProxyRecipients`.
  - *Giải pháp (Architecture hack):* Tại màn hình Check-in, `SearchShareholdersHandler` tự động GroupBy CCCD để không hiển thị trùng lặp. Khi click chọn, `IdentifyCheckinSituationHandler` tìm **TẤT CẢ** bản ghi trùng lặp, "hút" toàn bộ proxies của họ, tạo JIT một `Shareholder` ảo (0 CP) và gán proxies vào đó. Khách mời biến thành Cổ đông F2 mượt mà không cần thay đổi Schema Database.

---
