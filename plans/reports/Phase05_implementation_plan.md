# Phase 05 — Unit + Integration + E2E Playwright Tests, Performance Hardening & Pilot Demo

## Bối cảnh

Phase 05 là **quality gate** cuối cùng của pilot 6 tuần. Không pass → không approve plan full 7 tháng. Cần viết đủ test coverage cho code Phase 01→05, chạy performance benchmark, và chuẩn bị demo trên fresh Windows machine.

## Phân tích GAP hiện tại

| Hạng mục | Hiện tại | Yêu cầu Phase 05 |
|---|---|---|
| Unit Tests | 0 (placeholder `UnitTest1.cs`) | ~23 test cases (VsdcParser 8 + VsdcValidator 6 + VsdcRowMapper 4 + Validators 5) |
| Integration Tests | 3 tests (Phase01Tests.cs) | ~11 test cases mới (Auth 3 + Meeting CRUD 5 + Import 3) |
| E2E Tests | 0 (placeholder `UnitTest1.cs`) | 4 Playwright scenarios + 4 Page Objects |
| Performance | Chưa đo | Benchmark 1,000 CĐ < 10s |
| Coverage | Chưa đo | ≥ 70% Domain + Application |
| CI | Build + Unit/Integration (chung 1 job) | Tách riêng integration job + E2E job |
| Demo Docs | Chưa có | quick-start-guide.md + pilot-demo-checklist.md |

## Open Questions

> [!IMPORTANT]
> **Q1:** E2E Playwright cần Testcontainers Postgres hay SQLite in-memory đủ? Đề xuất Postgres để test đúng behavior upsert/raw SQL.

> [!IMPORTANT]
> **Q2:** Coverage tool chỉ dùng Coverlet built-in + ReportGenerator local, hay cần upload lên Codecov/SonarQube?

> [!NOTE]
> **Q3:** Demo có cần quay video (OBS Studio) để share với stakeholders không?

---

## Proposed Changes

### Component 1: Mms.UnitTests (Unit Test Project)

#### [MODIFY] [Mms.UnitTests.csproj](file:///d:/PROJECT/Robotia_AGM_Voting/tests/Mms.UnitTests/Mms.UnitTests.csproj)
- Thêm `ProjectReference` → `Mms.Infrastructure` (cần test VsdcParser, VsdcValidator, VsdcRowMapper)
- Thêm NuGet: `Moq` (mock dependencies)
- Thêm NuGet: `ClosedXML` (tạo test xlsx in-memory)

#### [DELETE] [UnitTest1.cs](file:///d:/PROJECT/Robotia_AGM_Voting/tests/Mms.UnitTests/UnitTest1.cs)
- Xóa placeholder

#### [NEW] `tests/Mms.UnitTests/Parsing/VsdcParserTests.cs`
8 test cases:
1. `ParseSampleFile_Returns_CorrectRowCount` — happy path
2. `ParseSampleFile_Column5_IsIdNumber`
3. `ParseSampleFile_Column10_IsNationality`
4. `ParseSampleFile_Column16_VotingRightsPositive`
5. `ParseEmptyFile_Returns_EmptyList` — file rỗng
6. `ParseFileWithoutHeader_Throws_VsdcFormatException` — file không có header
7. `ParseCommaFormattedNumber_ParsesCorrectly` — số VN "18.600"
8. `ParseFileWithEmptyRowInMiddle_StopsAtEmptyRow`

> Dùng `ClosedXML` tạo file .xlsx in-memory thay vì dùng file tĩnh, giúp test portable và self-contained.

#### [NEW] `tests/Mms.UnitTests/Parsing/VsdcValidatorTests.cs`
6 test cases:
1. `Validate_MissingIdNumber_Returns_Warning`
2. `Validate_MissingName_Returns_Warning`
3. `Validate_ZeroVotingRights_Returns_Warning`
4. `Validate_DuplicateIdNumber_Returns_Warning`
5. `Validate_TotalExceedsVDL_Returns_Warning`
6. `Validate_AllValid_Returns_NoErrorsOrWarnings`

> Lưu ý: VsdcValidator hiện tại trả tất cả rules dưới dạng **warning** (không error), test phải reflect đúng behavior thực tế.

#### [NEW] `tests/Mms.UnitTests/Parsing/VsdcRowMapperTests.cs`
4 test cases:
1. `Map_ValidRow_ReturnsCorrectDto` — happy path
2. `Map_NullFields_HandlesGracefully` — null-safe
3. `Map_VietnamDateFormat_ParsesCorrectly` — dd/MM/yyyy
4. `Map_OADateFormat_ParsesCorrectly` — Excel OADate

#### [NEW] `tests/Mms.UnitTests/Validators/CreateMeetingValidatorTests.cs`
5 test cases:
1. `Title_Empty_ReturnsError`
2. `MeetingDate_InPast_ReturnsError`
3. `Location_Empty_ReturnsError`
4. `TotalVotingShares_Zero_ReturnsError`
5. `ValidMeeting_ReturnsNoErrors`

#### [NEW] `tests/Mms.UnitTests/Validators/UpsertCompanyValidatorTests.cs`
5 test cases:
1. `TaxCode_WrongFormat_ReturnsError` — không phải 10/13 digits
2. `CharterCapital_Zero_ReturnsError`
3. `CompanyName_Empty_ReturnsError`
4. `ValidCompany_ReturnsNoErrors`
5. `TaxCode_13Digits_ReturnsNoErrors`

---

### Component 2: Mms.IntegrationTests (Integration Test Project)

#### [MODIFY] [Mms.IntegrationTests.csproj](file:///d:/PROJECT/Robotia_AGM_Voting/tests/Mms.IntegrationTests/Mms.IntegrationTests.csproj)
- Thêm NuGet: `MediatR` (send commands qua pipeline)
- Thêm `ProjectReference` → `Mms.Web` (cần `WebApplicationFactory` / DI container đầy đủ)

#### [MODIFY] [DatabaseFixture.cs](file:///d:/PROJECT/Robotia_AGM_Voting/tests/Mms.IntegrationTests/Fixtures/DatabaseFixture.cs)
- Expose `IServiceProvider` tốt hơn (nếu cần MediatR `ISender`)
- Đảm bảo `ISender` (MediatR) available từ DI container

#### [NEW] `tests/Mms.IntegrationTests/Auth/AuthFlowIntegrationTests.cs`
3 test cases:
1. `LoginWithCorrectCredentials_ReturnsJwtToken`
2. `LoginWithWrongPassword_Returns401`
3. `ChangePassword_ClearsMustChangePasswordFlag`

#### [NEW] `tests/Mms.IntegrationTests/Meetings/MeetingCrudIntegrationTests.cs`
5 test cases:
1. `CreateMeeting_WithResolutionsAndCandidates_PersistsAll`
2. `UpdateMeeting_ChangesTitle_AuditLogCreated`
3. `DeleteMeeting_WithoutShareholders_SoftDeletes`
4. `DeleteMeeting_WithShareholders_ThrowsBusinessException`
5. `CloneMeeting_CopiesResolutionsAndCandidates_NotShareholders`

#### [NEW] `tests/Mms.IntegrationTests/Import/ImportFlowIntegrationTests.cs`
3 test cases (bao gồm performance):
1. `ImportThousandRows_CompletesUnder10Seconds` — **performance gate**
2. `ImportSameFileTwice_SecondImport_ReplacesAll`
3. `ImportWithDbError_RollsBackTransaction`

#### [NEW] `tests/Mms.IntegrationTests/Import/TestData/` (fixture files)
- Tạo synthetic data generator 1,000 rows (in-code, không dùng file tĩnh)
- Tạo `vsdc-sample-invalid-rows.xlsx` (2 invalid rows: thiếu ID + CP=0)

---

### Component 3: Mms.E2ETests (E2E Playwright Project)

#### [MODIFY] [Mms.E2ETests.csproj](file:///d:/PROJECT/Robotia_AGM_Voting/tests/Mms.E2ETests/Mms.E2ETests.csproj)
- Thêm NuGet: `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`
- Thêm `ProjectReference` → `Mms.Web`
- Thêm trait `[Trait("Category", "E2E")]` cho tất cả test classes

#### [DELETE] [UnitTest1.cs](file:///d:/PROJECT/Robotia_AGM_Voting/tests/Mms.E2ETests/UnitTest1.cs)

#### [NEW] `tests/Mms.E2ETests/Fixtures/PlaywrightFixture.cs`
- `WebApplicationFactory<Program>` start Blazor Server test instance
- Playwright.CreateAsync → Chromium headless
- Testcontainers Postgres (hoặc reuse CI service)

#### [NEW] `tests/Mms.E2ETests/PageObjects/LoginPage.cs`
- `NavigateAsync()`, `FillCredentials()`, `SubmitAsync()`, `GetErrorMessage()`

#### [NEW] `tests/Mms.E2ETests/PageObjects/DashboardPage.cs`
- `IsVisible()`, `GetMeetingCount()`

#### [NEW] `tests/Mms.E2ETests/PageObjects/MeetingFormPage.cs`
- `FillTitle()`, `SelectType()`, `FillDate()`, `AddResolution()`, `SubmitAsync()`

#### [NEW] `tests/Mms.E2ETests/PageObjects/ImportWizardPage.cs`
- `Step1_UploadFile()`, `Step2_ClickNext()`, `Step3_VerifyNoErrors()`, `Step3_ClickImport()`, `Step4_GetInsertedCount()`

#### [NEW] `tests/Mms.E2ETests/Scenarios/LoginScenarioTests.cs`
2 tests: happy path + wrong password

#### [NEW] `tests/Mms.E2ETests/Scenarios/CreateMeetingScenarioTests.cs`
1 test: tạo meeting → verify trong list

#### [NEW] `tests/Mms.E2ETests/Scenarios/ImportVsdcHappyPathTests.cs`
1 test: upload file → import → verify count

#### [NEW] `tests/Mms.E2ETests/Scenarios/ImportVsdcValidationErrorTests.cs`
1 test: upload invalid file → verify error count + import button disabled

---

### Component 4: CI/CD

#### [MODIFY] [ci-build-test.yml](file:///d:/PROJECT/Robotia_AGM_Voting/.github/workflows/ci-build-test.yml)
- Tách `integration-tests` thành job riêng (cần Docker)
- Thêm `e2e-tests` job (cần Playwright install)
- Code coverage upload step

---

### Component 5: Documentation

#### [NEW] `docs/quick-start-guide.md`
- Hướng dẫn từ zero → running demo (cài Docker, clone, `.env`, docker-compose up)

#### [NEW] `docs/pilot-demo-checklist.md`
- Step-by-step demo script cho pilot review meeting
- Pre-demo checklist, demo steps, post-demo verification

---

## Execution Order (12 Bước)

| Bước | Nội dung | Est. Time |
|------|----------|-----------|
| 1 | Unit Tests — VsdcParser (8 cases) | 1.5h |
| 2 | Unit Tests — VsdcValidator (6 cases) | 1h |
| 3 | Unit Tests — VsdcRowMapper (4 cases) | 0.5h |
| 4 | Unit Tests — FluentValidation (Meeting + Company, 10 cases) | 1h |
| 5 | Integration Tests — DatabaseFixture upgrade + Auth Flow (3 cases) | 1.5h |
| 6 | Integration Tests — Meeting CRUD (5 cases) | 1.5h |
| 7 | Integration Tests — Import Flow + Performance benchmark (3 cases) | 2h |
| 8 | E2E Playwright — Fixture + Page Objects setup | 2h |
| 9 | E2E Playwright — 4 Scenarios | 2h |
| 10 | Performance Hardening (nếu benchmark fail) | 1-3h |
| 11 | Code Coverage Report + CI workflow update | 1h |
| 12 | Demo Docs (quick-start + checklist) | 1h |

**Tổng ước tính: 15-17h** (2-3 ngày làm việc)

---

## Verification Plan

### Automated Tests
```bash
# 1. Chạy Unit Tests (< 30s)
dotnet test tests/Mms.UnitTests/ --configuration Release --verbosity normal

# 2. Chạy Integration Tests (< 3 min, cần Docker)
dotnet test tests/Mms.IntegrationTests/ --configuration Release --verbosity normal

# 3. Chạy E2E Tests (< 5 min, cần Docker + Playwright)
dotnet test tests/Mms.E2ETests/ --configuration Release --verbosity normal

# 4. Code Coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage --filter "Category!=E2E"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage/**/*.xml" -targetdir:"coverage/report" -reporttypes:Html
```

### Performance Gate
- Import 1,000 synthetic CĐ < 10s (assertion trong `ImportFlowIntegrationTests`)
- Nếu fail → implement fallback: Npgsql `COPY` hoặc `EFCore.BulkExtensions`

### Manual Verification
- `docker-compose up -d` trên fresh Windows VM → app hoạt động < 60s
- Chạy full demo checklist end-to-end
- Verify audit log ghi đầy đủ
