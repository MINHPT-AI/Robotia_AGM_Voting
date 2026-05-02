# Phase 05 — Unit + Integration + E2E Playwright Tests, Performance Hardening & Pilot Demo

## Context Links

- Parent plan: [`./plan.md`](./plan.md)
- Dependency: tất cả phase-01 → phase-04 phải hoàn thành
- Brainstorm: [`../reports/brainstorm-260420-1558-agm-voting-system-architecture.md`](../reports/brainstorm-260420-1558-agm-voting-system-architecture.md) § 5 (Risks), § 6 (Success Metrics), § 4 (NFR)
- BRD: [`../../brd-quy-trinh-dhcd.md`](../../brd-quy-trinh-dhcd.md) — toàn bộ Bước 1 + Bước 2
- UI Spec: [`../../ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md`](../../ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md) — A1, B1, B2, C1, C2
- Test samples: [`../../ExempleTemplate_file/Mẫu file DS VSDC gui.xlsx`](../../ExempleTemplate_file/Mẫu%20file%20DS%20VSDC%20gui.xlsx)

---

## Overview

- **Tuần**: 6 (tuần cuối pilot)
- **Priority**: P1 (gate để approve full 7-month plan)
- **Status**: pending
- **Brief**: Viết đủ test coverage cho toàn bộ code đã implement (phase-01 → 04), chạy performance benchmark (1,000 CĐ < 10s), hardening edge cases, chuẩn bị demo checklist trên fresh Windows machine. Phase này là "quality gate" — không pass test thì không approve plan tiếp.

---

## Key Insights

- **Test pyramid**: Unit (nhiều, nhanh) → Integration (vừa, Testcontainers) → E2E (ít, chậm, Playwright). Tỷ lệ đề xuất: 60/30/10.
- **Testcontainers strategy**: mỗi integration test class dùng chung 1 `DatabaseFixture` (PostgreSqlContainer) spin up 1 lần per test class (không per test method) để tiết kiệm thời gian.
- **Playwright E2E**: 4 scenario chính đủ để cover flow pilot end-to-end. Không over-test UI details — focus vào happy paths + 1 sad path per critical feature.
- **Performance benchmark**: đo bằng `Stopwatch` trong integration test (`Assert.True(elapsed < 10s)`). Nếu fail → profiling EF query, thêm batch index, hoặc upgrade sang `EFCore.BulkExtensions`.
- **Demo environment**: chuẩn bị fresh VM (Windows 10/11, Docker Desktop fresh install) để prove "docker-compose up → working" claim trong exit criteria.
- **Code coverage**: target 70% cho `Mms.Domain` + `Mms.Application`. `Mms.Infrastructure` và `Mms.Web` lower OK (integration + E2E cover). Đo bằng `dotnet test --collect:"XPlat Code Coverage"` + ReportGenerator.
- **Edge cases parser phải test**: file rỗng, file có 0 data rows, dòng trống xen kẽ, merged cells header, encoding tiếng Việt, số CP dạng string "1,000" (comma-formatted).

---

## Requirements

### Functional

- [F-05.1] Unit test `VsdcParser` ≥ 8 cases (happy path + edge cases).
- [F-05.2] Unit test `VsdcValidator` ≥ 6 cases (mỗi validation rule ≥ 1 case).
- [F-05.3] Unit test `VsdcRowMapper` ≥ 4 cases (parse types, null-safe, date format).
- [F-05.4] Unit test validators FluentValidation (Meeting, Company) ≥ 5 cases mỗi validator.
- [F-05.5] Integration test import flow end-to-end (Testcontainers Postgres).
- [F-05.6] Integration test auth flow (login, JWT, change password).
- [F-05.7] Integration test meeting CRUD (create, update, soft-delete, clone).
- [F-05.8] E2E Playwright: 4 scenarios (xem chi tiết §Implementation Steps).
- [F-05.9] Performance test: import 1,000 synthetic CĐ < 10s (assertion trong integration test).
- [F-05.10] Demo checklist doc: step-by-step từ fresh Windows → working demo.

### Non-Functional

- [NF-05.1] Unit test suite chạy < 30s (không I/O, không DB).
- [NF-05.2] Integration test suite chạy < 3 phút (Testcontainers warm-up included).
- [NF-05.3] E2E Playwright chạy < 5 phút (4 scenarios, headless).
- [NF-05.4] Code coverage ≥ 70% cho Domain + Application layers.
- [NF-05.5] 0 failing test khi merge vào `main`.

---

## Architecture

### Test Project Structure

```
tests/
├── Mms.UnitTests/
│   ├── Parsing/
│   │   ├── VsdcParserTests.cs
│   │   ├── VsdcRowMapperTests.cs
│   │   └── VsdcValidatorTests.cs
│   ├── Validators/
│   │   ├── CreateMeetingValidatorTests.cs
│   │   ├── UpsertCompanyValidatorTests.cs
│   │   └── ImportShareholdersValidatorTests.cs
│   └── Domain/
│       └── MeetingStatusTransitionTests.cs    # nếu có domain logic
│
├── Mms.IntegrationTests/
│   ├── Fixtures/
│   │   └── DatabaseFixture.cs                 # Testcontainers PostgreSqlContainer
│   ├── Auth/
│   │   └── AuthFlowIntegrationTests.cs
│   ├── Meetings/
│   │   └── MeetingCrudIntegrationTests.cs
│   └── Import/
│       ├── ImportFlowIntegrationTests.cs      # end-to-end + perf
│       └── TestData/
│           ├── vsdc-sample-1000-rows.xlsx     # generated fixture
│           └── vsdc-sample-invalid-rows.xlsx
│
└── Mms.E2ETests/
    ├── Fixtures/
    │   └── PlaywrightFixture.cs               # app startup + browser
    ├── Scenarios/
    │   ├── LoginScenarioTests.cs
    │   ├── CreateMeetingScenarioTests.cs
    │   ├── ImportVsdcHappyPathTests.cs
    │   └── ImportVsdcValidationErrorTests.cs
    └── PageObjects/                           # Page Object Model
        ├── LoginPage.cs
        ├── DashboardPage.cs
        ├── MeetingFormPage.cs
        └── ImportWizardPage.cs
```

### Testcontainers Flow

```
[CollectionDefinition] DatabaseCollection
    └── DatabaseFixture (IAsyncLifetime)
            ├── OnStartAsync:  PostgreSqlContainer.StartAsync()
            │                  → apply EF migration
            │                  → seed data
            └── OnStopAsync:  PostgreSqlContainer.StopAsync()

[Collection("Database")] IntegrationTestClass
    ├── Constructor: inject DatabaseFixture
    └── Each test: dùng chung container (không recreate)
```

### Playwright Flow

```
PlaywrightFixture (IAsyncLifetime)
    ├── OnStartAsync:
    │   ├── Start test server: WebApplicationFactory<Program>
    │   └── Playwright.CreateAsync() → IPlaywright
    │       └── playwright.Chromium.LaunchAsync(headless: true)
    └── Each scenario: new Page per test
```

---

## Related Code Files

### Tạo mới

```
tests/Mms.UnitTests/
├── Mms.UnitTests.csproj                       # xUnit, FluentAssertions, Moq
├── Parsing/VsdcParserTests.cs
├── Parsing/VsdcRowMapperTests.cs
├── Parsing/VsdcValidatorTests.cs
├── Validators/CreateMeetingValidatorTests.cs
└── Validators/UpsertCompanyValidatorTests.cs

tests/Mms.IntegrationTests/
├── Mms.IntegrationTests.csproj                # Testcontainers.PostgreSql, MediatR
├── Fixtures/DatabaseFixture.cs
├── Auth/AuthFlowIntegrationTests.cs
├── Meetings/MeetingCrudIntegrationTests.cs
├── Import/ImportFlowIntegrationTests.cs
└── Import/TestData/                           # fixture files

tests/Mms.E2ETests/
├── Mms.E2ETests.csproj                        # Microsoft.Playwright, xUnit
├── Fixtures/PlaywrightFixture.cs
├── Scenarios/LoginScenarioTests.cs
├── Scenarios/CreateMeetingScenarioTests.cs
├── Scenarios/ImportVsdcHappyPathTests.cs
├── Scenarios/ImportVsdcValidationErrorTests.cs
└── PageObjects/*.cs

docs/
├── quick-start-guide.md                       # từ zero → demo
└── pilot-demo-checklist.md                    # step-by-step demo script
```

### Sửa

```
.github/workflows/ci-build-test.yml            # thêm integration test step (có Docker)
README.md                                      # cập nhật test instructions
```

---

## Implementation Steps

### Bước 1: Unit Tests — VsdcParser

Tạo `VsdcParserTests.cs` với các cases sau (dùng file `.xlsx` thật từ `ExempleTemplate_file/`):

```csharp
// TC-01: Happy path — sample file đọc đúng số rows
[Fact] ParseSampleFile_Returns_CorrectRowCount()

// TC-02: Cột 5 (id_number) đọc đúng giá trị
[Fact] ParseSampleFile_Column5_IsIdNumber()

// TC-03: Cột 10 (nationality) đọc đúng
[Fact] ParseSampleFile_Column10_IsNationality()

// TC-04: Cột 16 (voting_rights) là số dương
[Fact] ParseSampleFile_Column16_VotingRightsPositive()

// TC-05: File rỗng (0 data rows) → trả danh sách rỗng, không throw
[Fact] ParseEmptyFile_Returns_EmptyList()

// TC-06: File không có header VSDC → throw VsdcFormatException
[Fact] ParseFileWithoutHeader_Throws_VsdcFormatException()

// TC-07: Số CP dạng string "1,000" (comma) → parse thành 1000 (long)
[Fact] ParseCommaFormattedNumber_ParsesCorrectly()

// TC-08: Dòng trống xen kẽ giữa data → dừng đúng chỗ (không đọc sau dòng trống)
[Fact] ParseFileWithEmptyRowInMiddle_StopsAtEmptyRow()
```

### Bước 2: Unit Tests — VsdcValidator

```csharp
// TC-01: Row thiếu id_number → error MISSING_ID_NUMBER
[Fact] Validate_MissingIdNumber_Returns_Error()

// TC-02: Row thiếu full_name → error MISSING_NAME
[Fact] Validate_MissingName_Returns_Error()

// TC-03: Row voting_rights = 0 → error ZERO_VOTING_RIGHTS
[Fact] Validate_ZeroVotingRights_Returns_Error()

// TC-04: id_number đã tồn tại trong DB → warning DUPLICATE_ID_NUMBER
[Fact] Validate_DuplicateIdNumber_Returns_Warning()

// TC-05: Tổng CP > VĐL → warning EXCEEDS_CHARTER_CAPITAL
[Fact] Validate_TotalExceedsVDL_Returns_Warning()

// TC-06: File hợp lệ hoàn toàn → 0 errors, 0 warnings
[Fact] Validate_AllValid_Returns_NoErrorsOrWarnings()
```

### Bước 3: Unit Tests — FluentValidation (Meeting + Company)

```csharp
// Meeting
CreateMeetingValidator: title required, date > today, location required,
                        total_voting_shares > 0, total_voting_shares <= company.total_shares

// Company
UpsertCompanyValidator: tax_code format (10 or 13 digits), charter_capital > 0,
                        total_voting_shares <= total_shares_issued
```

### Bước 4: Integration Tests — DatabaseFixture

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public MmsDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();
        var options = new DbContextOptionsBuilder<MmsDbContext>()
            .UseNpgsql(_pgContainer.GetConnectionString())
            .Options;
        DbContext = new MmsDbContext(options);
        await DbContext.Database.MigrateAsync();
        await SeedData.EnsureSeededAsync(DbContext); // roles + admin user
    }

    public async Task DisposeAsync() => await _pgContainer.StopAsync();
}
```

### Bước 5: Integration Tests — Import Flow + Performance

```csharp
[Collection("Database")]
public class ImportFlowIntegrationTests(DatabaseFixture fixture)
{
    // TC-01: Import 1,000 synthetic rows → inserted=1000, updated=0, < 10s
    [Fact]
    public async Task ImportThousandRows_CompletesUnder10Seconds()
    {
        var rows = GenerateSyntheticRows(1000);
        var sw = Stopwatch.StartNew();
        var result = await _mediator.Send(new ImportShareholdersCommand(meetingId, rows));
        sw.Stop();

        Assert.Equal(1000, result.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10),
            $"Import took {sw.Elapsed.TotalSeconds:F1}s — expected < 10s");
    }

    // TC-02: Import cùng file 2 lần → lần 2: inserted=0, updated=1000
    [Fact]
    public async Task ImportSameFileTwice_SecondImport_UpdatesAll()

    // TC-03: Import bị rollback khi DB error giữa chừng → bảng trống
    [Fact]
    public async Task ImportWithDbError_RollsBackTransaction()

    // Helper
    private List<ShareholderImportDto> GenerateSyntheticRows(int count) =>
        Enumerable.Range(1, count).Select(i => new ShareholderImportDto {
            FullName = $"Cổ Đông {i:D5}",
            IdNumber = $"ID{i:D10}",
            Nationality = "Việt Nam",
            VotingRights = 1000 + i,
            // ... other fields
        }).ToList();
}
```

### Bước 6: Integration Tests — Auth + Meeting CRUD

```csharp
// Auth
AuthFlowIntegrationTests:
  - LoginWithCorrectCredentials_ReturnsJwtToken()
  - LoginWithWrongPassword_Returns401()
  - ChangePassword_ClearsMustChangePasswordFlag()

// Meeting
MeetingCrudIntegrationTests:
  - CreateMeeting_WithResolutionsAndCandidates_PersistsAll()
  - UpdateMeeting_ChangesTitle_AuditLogCreated()
  - DeleteMeeting_WithoutShareholders_SoftDeletes()
  - DeleteMeeting_WithShareholders_ThrowsBusinessException()
  - CloneMeeting_CopiesResolutionsAndCandidates_NotShareholders()
```

### Bước 7: E2E Playwright — Setup

1. Cài NuGet: `Microsoft.Playwright`, `Microsoft.Playwright.NUnit` (hoặc xUnit).
2. Install browsers: `playwright install chromium` (trong Dockerfile CI).
3. `PlaywrightFixture.cs`:
   - Start `WebApplicationFactory<Program>` với test DB (Testcontainers hoặc SQLite in-memory).
   - `Playwright.CreateAsync()` → `playwright.Chromium.LaunchAsync(new() { Headless = true })`.
4. `Page Object Model`: mỗi Page Object wrap `IPage`, expose actions `FillUsername()`, `ClickLogin()`, `GetErrorMessage()`, v.v.

### Bước 8: E2E Playwright — 4 Scenarios

**Scenario 1 — Login Happy Path + Wrong Password**:
```csharp
[Fact] async Task Login_CorrectCredentials_RedirectsToDashboard()
{
    await _loginPage.NavigateAsync();
    await _loginPage.FillCredentials("admin", "Admin@2026!");
    await _loginPage.SubmitAsync();
    // Redirect to change-password (MustChangePassword=true)
    Assert.Contains("/change-password", _page.Url);
}

[Fact] async Task Login_WrongPassword_ShowsErrorToast()
{
    await _loginPage.FillCredentials("admin", "WrongPass");
    await _loginPage.SubmitAsync();
    var toast = await _page.Locator(".mud-snackbar").TextContentAsync();
    Assert.Contains("không đúng", toast);
}
```

**Scenario 2 — Create Meeting End-to-End**:
```csharp
[Fact] async Task CreateMeeting_WithResolution_AppearsInList()
{
    await LoginAs("admin");
    await _page.GotoAsync("/meetings/new");
    await _meetingForm.FillTitle("ĐHCĐ TN Test 2026");
    await _meetingForm.SelectType("Thường niên");
    await _meetingForm.FillDate(DateTime.Today.AddMonths(2));
    await _meetingForm.FillLocation("Hội trường A");
    await _meetingForm.AddResolution("NQ1", "Thông qua BCTC");
    await _meetingForm.SubmitAsync();
    // Verify in list
    await _page.GotoAsync("/meetings");
    Assert.True(await _page.Locator("text=ĐHCĐ TN Test 2026").IsVisibleAsync());
}
```

**Scenario 3 — Import VSDC Happy Path**:
```csharp
[Fact] async Task ImportVsdc_ValidFile_ShowsCorrectCount()
{
    var meetingId = await CreateTestMeeting();
    await _page.GotoAsync($"/meetings/{meetingId}/import");
    await _importWizard.Step1_UploadFile("ExempleTemplate_file/Mẫu file DS VSDC gui.xlsx");
    await _importWizard.Step2_ClickNext(); // preview map
    await _importWizard.Step3_VerifyNoErrors();
    await _importWizard.Step3_ClickImport();
    var insertedCount = await _importWizard.Step4_GetInsertedCount();
    Assert.True(insertedCount > 0);
}
```

**Scenario 4 — Import VSDC Invalid File (Validation Errors)**:
```csharp
[Fact] async Task ImportVsdc_FileWithErrors_ShowsValidationErrors()
{
    // dùng vsdc-sample-invalid-rows.xlsx (có row thiếu CMND + row CP=0)
    await _importWizard.Step1_UploadFile("TestData/vsdc-sample-invalid-rows.xlsx");
    await _importWizard.Step2_ClickNext();
    var errorCount = await _importWizard.Step3_GetErrorCount();
    Assert.True(errorCount > 0);
    // Import button disabled
    Assert.False(await _importWizard.Step3_IsImportButtonEnabled());
}
```

### Bước 9: Performance Hardening

Nếu benchmark import > 10s, thực hiện theo thứ tự:

1. **Profiling**: thêm `MiniProfiler` (dev only) để xem query thời gian.
2. **Batch size**: giảm từ 500 → 200 (ít parameter per query).
3. **Disable EF tracking**: `AsNoTracking()` + `AutoDetectChangesEnabled = false`.
4. **PostgreSQL COPY** (nếu cần): Npgsql `NpgsqlBinaryImporter`:
   ```csharp
   await using var writer = conn.BeginBinaryImport(
       "COPY shareholders (meeting_id, id_number, full_name, ...) FROM STDIN (FORMAT BINARY)");
   foreach (var s in shareholders) { writer.StartRow(); writer.Write(s.MeetingId); ... }
   await writer.CompleteAsync();
   ```
5. **Index check**: verify `EXPLAIN ANALYZE` trên upsert query — ensure index `(meeting_id, id_number)` used.

### Bước 10: Code Coverage Report

```bash
dotnet test --collect:"XPlat Code Coverage" \
  --results-directory ./coverage \
  --filter "Category!=E2E"

dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"coverage/**/*.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:Html

# Open coverage/report/index.html
```

Target: Domain ≥ 70%, Application ≥ 70%.

### Bước 11: CI — Thêm Integration Test Step

Cập nhật `.github/workflows/ci-build-test.yml`:
```yaml
  integration-tests:
    runs-on: ubuntu-latest   # Docker available on ubuntu runner
    needs: build-and-test
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet test tests/Mms.IntegrationTests/ \
               --configuration Release \
               --logger "trx;LogFileName=integration-results.xml"
      # Note: E2E tests run manually / separate job

  e2e-tests:
    runs-on: ubuntu-latest
    needs: integration-tests
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet build
      - run: npx playwright install chromium
      - run: dotnet test tests/Mms.E2ETests/ \
               --configuration Release \
               --logger "trx;LogFileName=e2e-results.xml"
```

### Bước 12: Demo Checklist + Quick Start Guide

Tạo `docs/pilot-demo-checklist.md`:

```markdown
# Pilot Demo Checklist

## Pre-demo (1 ngày trước)
- [ ] Fresh Windows 10/11 VM (hoặc machine)
- [ ] Cài Docker Desktop (download offline nếu LAN)
- [ ] Clone repo / copy USB
- [ ] Tạo `.env` từ `.env.example` (đổi JWT secret)

## Demo Steps
1. [ ] `docker-compose up -d` → chờ healthy (< 60s)
2. [ ] Mở browser http://localhost:8080
3. [ ] Login: admin / Admin@2026! → đổi mật khẩu → vào Dashboard
4. [ ] Vào "Thông tin DN" → điền thông tin công ty → Lưu
5. [ ] Vào "Cuộc họp" → Tạo mới → điền form + 2 resolutions → Lưu
6. [ ] Vào Import VSDC → upload `Mẫu file DS VSDC gui.xlsx`
        → Step 2 preview 16 cột → Step 3 validate (0 lỗi) → Import
        → Step 4: hiện số CĐ đã import (< 10s)
7. [ ] Quay về Dashboard → hiển thị meeting vừa tạo
8. [ ] Logout → verify redirect login

## Post-demo
- [ ] `docker-compose down` → verify data vẫn còn khi `docker-compose up` lại
- [ ] Export audit log → show trail đầy đủ
```

---

## Todo List

- [ ] Tạo Mms.UnitTests.csproj + cài xUnit, FluentAssertions, Moq
- [ ] Tạo VsdcParserTests.cs (8 test cases)
- [ ] Tạo VsdcValidatorTests.cs (6 test cases)
- [ ] Tạo VsdcRowMapperTests.cs (4 test cases)
- [ ] Tạo CreateMeetingValidatorTests.cs (5 cases)
- [ ] Tạo UpsertCompanyValidatorTests.cs (5 cases)
- [ ] Tạo Mms.IntegrationTests.csproj + cài Testcontainers.PostgreSql
- [ ] Tạo DatabaseFixture.cs (spin up Postgres + migrate + seed)
- [ ] Tạo AuthFlowIntegrationTests.cs (3 cases)
- [ ] Tạo MeetingCrudIntegrationTests.cs (5 cases)
- [ ] Tạo ImportFlowIntegrationTests.cs (3 cases gồm perf test)
- [ ] Tạo synthetic data generator 1,000 rows
- [ ] Tạo vsdc-sample-invalid-rows.xlsx fixture (có 2 invalid rows)
- [ ] Verify import 1,000 rows < 10s → nếu fail, profile + optimize
- [ ] Tạo Mms.E2ETests.csproj + cài Microsoft.Playwright
- [ ] Tạo PlaywrightFixture.cs + WebApplicationFactory setup
- [ ] Tạo Page Objects: LoginPage, DashboardPage, MeetingFormPage, ImportWizardPage
- [ ] Tạo 4 Playwright scenario tests
- [ ] Chạy coverage report → verify Domain ≥ 70%, Application ≥ 70%
- [ ] Cập nhật CI workflow: thêm integration + E2E jobs
- [ ] Tạo docs/quick-start-guide.md
- [ ] Tạo docs/pilot-demo-checklist.md
- [ ] Chạy full demo trên fresh Windows VM → pass checklist
- [ ] Toàn bộ tests xanh trên CI

---

## Success Criteria (Exit Criteria Pilot)

- [ ] **CI xanh**: tất cả unit + integration + E2E tests pass trên GitHub Actions.
- [ ] **Performance**: import 1,000 CĐ < 10s (assertion trong test, không chỉ manual).
- [ ] **docker-compose up** trên fresh Windows 10/11 + Docker Desktop → app sẵn sàng < 60s.
- [ ] **Login admin** → Dashboard hiển thị đúng.
- [ ] **Tạo meeting** với resolutions + candidates → lưu thành công.
- [ ] **Import VSDC** file sample → số CĐ đúng, validation đúng.
- [ ] **Code coverage** Domain + Application ≥ 70%.
- [ ] **Audit log** ghi đầy đủ cho create meeting + import.
- [ ] **Team review**: tech lead + 2 dev confirm confident với stack.
- [ ] **Decision**: approve plan full 7 tháng (hoặc ghi rõ concerns cần giải quyết trước).

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Playwright flaky tests (timing) | Medium | Dùng `WaitForSelectorAsync` thay fixed sleep; retry logic built-in Playwright |
| Testcontainers chậm trên CI (pull postgres:16 image) | Medium | Pre-pull image trong CI workflow hoặc dùng GitHub Actions cache |
| Import performance > 10s | High | Có sẵn fallback plan (BulkExtensions / Npgsql COPY) — implement nếu EF batch fail |
| Demo trên Windows VM thiếu Docker | Medium | Chuẩn bị USB installer Docker Desktop offline; test trước 1 ngày |
| E2E test fail do app startup slow | Low | `WebApplicationFactory` warm-up trong `PlaywrightFixture.InitializeAsync()` trước khi test |
| Coverage < 70% sau khi viết test | Medium | Dùng coverage report để xác định code path chưa cover → viết thêm edge case tests |

---

## Security Considerations

- Test data: không dùng data thật của khách hàng trong test fixtures. Dùng synthetic data hoặc ẩn danh hóa.
- Test DB credentials: dùng Testcontainers random port + random password — không hardcode.
- E2E: chạy headless, không capture screenshot chứa PII vào CI artifacts.
- Demo VM: xóa sau demo; không commit `.env` vào git.

---

## Next Steps

Sau khi pilot pass exit criteria:

1. **Họp team review**: trình bày kết quả pilot, test coverage report, performance benchmark.
2. **Decision gate**: approve full 7-month plan → tiến hành `/plan` cho Sprint 2-8:
   - Sprint 2: Template Engine (OpenXml + LibreOffice pool)
   - Sprint 3: Pre-meeting Proxy (Ủy quyền trước họp)
   - Sprint 4: Check-in POS + Ballot Lifecycle Cascade *(critical)*
   - Sprint 5: Qualification Report (Thẩm tra tư cách)
   - Sprint 6: Tallying / Kiểm phiếu (5 loại)
   - Sprint 7: Report Center + Display screens
   - Sprint 8: Load test + Packaging + Installer
3. **Ghi lại concerns** từ pilot: bất kỳ vấn đề nào phát hiện trong 6 tuần cần address trước Sprint 2.
4. **Pilot customer outreach**: liên hệ công ty pilot để schedule real-world test (ĐHCĐ thực).

---

## Unresolved Questions

1. **Playwright + WebApplicationFactory**: E2E test cần real Postgres hay SQLite in-memory đủ? Đề xuất: Postgres Testcontainers để test đúng behavior upsert.
2. **CI runner cho E2E**: GitHub Actions ubuntu runner hỗ trợ Playwright Chromium headless? (Có — đã được verify). Nhưng cần `playwright install` trong CI step.
3. **Coverage tool**: `Coverlet` (built-in) đủ dùng. Có cần upload lên Codecov hay SonarQube không?
4. **Demo record**: có cần quay video demo để share với stakeholder không? Dùng OBS Studio nếu cần.
