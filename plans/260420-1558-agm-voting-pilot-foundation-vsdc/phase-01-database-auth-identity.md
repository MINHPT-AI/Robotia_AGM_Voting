# Phase 01 — Database + Auth/Identity

## Context Links

- Parent plan: [`./plan.md`](./plan.md)
- Dependency: [`./phase-00-repo-solution-setup-docker-compose.md`](./phase-00-repo-solution-setup-docker-compose.md) (hoàn thành trước)
- Brainstorm: [`../reports/brainstorm-260420-1558-agm-voting-system-architecture.md`](../reports/brainstorm-260420-1558-agm-voting-system-architecture.md) § 3.3 (DB Schema), § 3.1 (Stack)
- BRD: [`../../brd-quy-trinh-dhcd.md`](../../brd-quy-trinh-dhcd.md) — Bước 1 (Thông tin DN + Cuộc họp)
- UI Spec sections: A1 Login, A2 Forgot Password, A3 Change Password (field definitions)

---

## Overview

- **Tuần**: 2
- **Priority**: P1 (unblock Blazor UI phase-02)
- **Status**: pending
- **Brief**: Thiết lập toàn bộ nền tảng dữ liệu: EF Core DbContext + Npgsql + initial migration tạo tất cả bảng cốt lõi (bao gồm skeleton bảng sẽ dùng ở phase sau). Cấu hình ASP.NET Core Identity với 4 roles. JWT token service. Serilog 3-sink config. Seed data admin.

---

## Key Insights

- **Tạo tất cả bảng ngay ở phase này** (dù phase sau mới dùng) để tránh migration conflict khi nhiều dev làm song song.
- **Postgres optimistic concurrency** dùng `xmin` system column (Npgsql native) cho Ballot entity — khai báo đúng mapping EF ngay từ đầu để không refactor sau.
- **Serilog Postgres sink** tự tạo bảng `logs` — KHÔNG cần migration thủ công. Chỉ cần connection string + `autoCreateSqlTable: true`.
- **Identity password hash**: dùng BCrypt qua `Bcrypt.Net-Next` thay vì default PBKDF2 — cấu hình `PasswordHasherCompatibilityMode` hoặc implement custom `IPasswordHasher`.
- **Force-change-password flow**: thêm claim `"MustChangePassword": "true"` vào JWT, middleware redirect về `/change-password` nếu claim tồn tại.
- **Migration strategy**: dùng `dotnet ef migrations add InitialCreate` từ project `Mms.Infrastructure`, output dir `Persistence/Migrations`.

---

## Requirements

### Functional

- [F-01.1] EF Core DbContext `MmsDbContext` kết nối Postgres qua Npgsql provider.
- [F-01.2] Migration `InitialCreate` tạo đủ tất cả bảng (xem danh sách § Architecture).
- [F-01.3] Seed data: 4 roles (`admin`, `operator`, `viewer`, `checkin`) + 1 admin user (username: `admin`, default password force-change).
- [F-01.4] ASP.NET Core Identity authenticate + authorize thành công.
- [F-01.5] JWT service phát token hợp lệ với claims: userId, username, role, mustChangePassword.
- [F-01.6] Refresh token lưu DB hoặc secure cookie (chọn secure cookie cho Blazor Server).
- [F-01.7] Serilog ghi log: (a) console structured, (b) rolling file `/logs/mms-.log` (daily, max 10 files), (c) Postgres sink bảng `logs`.
- [F-01.8] Integration test: connect Postgres → apply migration → seed → query thành công (Testcontainers).

### Non-Functional

- [NF-01.1] Migration apply thành công trong < 5s trên cold Postgres container.
- [NF-01.2] JWT token expiry: access 1h, refresh 8h (adjustable qua config).
- [NF-01.3] Password min 8 ký tự, 1 uppercase, 1 số (cơ bản — xem Unresolved Q1).
- [NF-01.4] Secrets (DB password, JWT secret) đọc từ env var / Docker secret — KHÔNG hardcode.
- [NF-01.5] Serilog không throw exception nếu Postgres sink unavailable (swallow sink error).

---

## Architecture

### DB Schema — tất cả bảng cần migration

```sql
-- Công ty (Phase 03)
companies (id UUID PK, name TEXT NOT NULL, short_name TEXT, tax_code TEXT NOT NULL UNIQUE,
           address TEXT, phone TEXT, email TEXT, fax TEXT, website TEXT, logo_path TEXT,
           stock_code TEXT, legal_rep_name TEXT NOT NULL, legal_rep_title TEXT NOT NULL,
           charter_capital BIGINT NOT NULL, total_shares_issued BIGINT NOT NULL,
           total_voting_shares BIGINT NOT NULL,
           created_at TIMESTAMPTZ DEFAULT now(), updated_at TIMESTAMPTZ)

-- Cuộc họp (Phase 03)
meetings (id UUID PK, company_id FK → companies,
          title TEXT NOT NULL, meeting_type TEXT NOT NULL, -- ANNUAL/EXTRAORDINARY
          status TEXT NOT NULL DEFAULT 'NEW',              -- lifecycle enum
          meeting_date TIMESTAMPTZ NOT NULL, location TEXT NOT NULL,
          record_date DATE NOT NULL,
          total_voting_shares BIGINT NOT NULL,
          chairman TEXT, secretary TEXT, notes TEXT,
          created_at TIMESTAMPTZ, updated_at TIMESTAMPTZ)

-- Nội dung biểu quyết (Phase 03)
meeting_resolutions (id UUID PK, meeting_id FK → meetings,
                     display_order INT NOT NULL, title TEXT NOT NULL, content TEXT,
                     created_at TIMESTAMPTZ)

-- Ứng viên bầu cử (Phase 03)
meeting_candidates (id UUID PK, meeting_id FK → meetings,
                    display_order INT NOT NULL, full_name TEXT NOT NULL,
                    position TEXT NOT NULL, -- HDQT/BKS
                    birth_year INT, notes TEXT,
                    created_at TIMESTAMPTZ)

-- Cổ đông VSDC (Phase 04)
shareholders (id UUID PK, meeting_id FK → meetings,
              vsdc_row INT,
              full_name TEXT NOT NULL,
              sid TEXT,
              investor_code TEXT,
              id_number TEXT NOT NULL,           -- Cột 5: CMND/CCCD/Passport
              id_issue_date DATE,
              address TEXT, email TEXT, phone TEXT,
              nationality TEXT,                  -- Cột 10: detect multilingual template
              shares_non_deposit BIGINT DEFAULT 0,
              shares_deposit BIGINT DEFAULT 0,
              shares_total BIGINT DEFAULT 0,
              rights_non_deposit BIGINT DEFAULT 0,
              rights_deposit BIGINT DEFAULT 0,
              voting_rights BIGINT NOT NULL,     -- Cột 16: MANDATORY
              imported_at TIMESTAMPTZ DEFAULT now(),
              UNIQUE(meeting_id, id_number))

-- Ủy quyền (Phase 05+)
proxies (id UUID PK, meeting_id FK, grantor_id FK → shareholders,
         grantee_name TEXT, grantee_id_number TEXT,
         shares BIGINT NOT NULL,
         scope TEXT NOT NULL, -- FULL/PARTIAL
         proxy_type TEXT NOT NULL, -- PRE_MEETING/ON_SITE
         proxy_date DATE, detail TEXT, scan_url TEXT,
         created_at TIMESTAMPTZ, invalidated_at TIMESTAMPTZ)

-- Phiếu bầu — Ballot Lifecycle (Phase 05+)
ballots (id UUID PK, meeting_id FK, shareholder_id FK,
         attend_code TEXT UNIQUE NOT NULL,
         voting_shares BIGINT NOT NULL,
         direct_shares BIGINT DEFAULT 0, proxy_shares BIGINT DEFAULT 0,
         status TEXT NOT NULL DEFAULT 'ACTIVE', -- ACTIVE/INVALIDATED/REGENERATED
         parent_ballot_id UUID NULL REFERENCES ballots(id),
         reprint_needed BOOL DEFAULT false,
         invalidation_reason TEXT,
         pos_terminal TEXT, operator_user_id UUID,
         created_at TIMESTAMPTZ, invalidated_at TIMESTAMPTZ, printed_at TIMESTAMPTZ,
         xmin xid)   -- Postgres system column for optimistic concurrency via Npgsql

-- Templates (Phase future)
templates (id UUID PK, meeting_id FK,
           template_type TEXT NOT NULL, -- 6 loại
           language TEXT NOT NULL,      -- VN/EN/DUAL
           version INT NOT NULL DEFAULT 1,
           file_path TEXT, fields_config JSONB,
           is_finalized BOOL DEFAULT false,
           uploaded_by UUID, uploaded_at TIMESTAMPTZ,
           UNIQUE(meeting_id, template_type, language, version))

-- Audit log (append-only — trigger chặn UPDATE/DELETE)
audit_logs (id BIGSERIAL PK, ts TIMESTAMPTZ DEFAULT now(),
            user_id UUID, actor TEXT NOT NULL,
            category TEXT NOT NULL,   -- CheckIn/Proxy/Ballot/CASCADE/Print/Report/Auth/System
            entity_type TEXT, entity_id UUID, meeting_id UUID,
            detail JSONB, pos_terminal TEXT)

-- Serilog sink: bảng `logs` tự tạo bởi sink, không cần migration
```

### Identity Schema (ASP.NET Core Identity managed)

```
AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims,
AspNetUserTokens, AspNetUserLogins
```

### Auth Flow (Blazor Server)

```
Browser ──POST /api/auth/login──► AuthController
                                    ├── PasswordSignIn (Identity)
                                    ├── Issue JWT access token (1h)
                                    ├── Set refresh token cookie (httpOnly, 8h)
                                    └── Return { accessToken, user }

Blazor Server page:
  ├── CascadingAuthenticationState
  ├── AuthorizeView (role-based visibility)
  └── [Authorize(Roles="admin,operator")] on page
```

---

## Related Code Files

### Tạo mới

```
src/Mms.Domain/
├── Entities/
│   ├── Company.cs
│   ├── Meeting.cs
│   ├── MeetingResolution.cs
│   ├── MeetingCandidate.cs
│   ├── Shareholder.cs          # skeleton — dùng đầy đủ ở phase-04
│   ├── Proxy.cs                # skeleton
│   ├── Ballot.cs               # skeleton — xmin mapping
│   ├── Template.cs             # skeleton
│   └── AuditLog.cs
├── Enums/
│   ├── MeetingStatus.cs        # NEW, PREPARING, CHECKIN, IN_SESSION, TALLYING, COMPLETED
│   ├── MeetingType.cs          # ANNUAL, EXTRAORDINARY
│   ├── ProxyType.cs            # PRE_MEETING, ON_SITE
│   ├── ProxyScope.cs           # FULL, PARTIAL
│   ├── BallotStatus.cs         # ACTIVE, INVALIDATED, REGENERATED
│   ├── TemplateType.cs         # INVITATION, VOTING_CARD, ... (6 loại)
│   └── AuditCategory.cs
└── Common/
    └── BaseEntity.cs           # Id (UUID), CreatedAt, UpdatedAt

src/Mms.Infrastructure/
├── Persistence/
│   ├── MmsDbContext.cs
│   ├── Migrations/             # auto-generated
│   ├── Configurations/         # IEntityTypeConfiguration per entity
│   │   ├── CompanyConfiguration.cs
│   │   ├── MeetingConfiguration.cs
│   │   ├── ShareholderConfiguration.cs
│   │   ├── BallotConfiguration.cs  # xmin mapping
│   │   └── AuditLogConfiguration.cs  # trigger no-update
│   └── SeedData.cs
├── Identity/
│   ├── ApplicationUser.cs      # extends IdentityUser
│   ├── ApplicationRole.cs      # extends IdentityRole
│   └── JwtTokenService.cs
└── Logging/
    └── SerilogConfiguration.cs

src/Mms.Web/
├── appsettings.json            # cấu hình JWT, DB, Serilog
├── appsettings.Development.json
└── Program.cs                  # DI registration
```

---

## Implementation Steps

### Bước 1: Entity + Enums (Domain)

1. Tạo `BaseEntity.cs` với `Guid Id`, `DateTime CreatedAt`, `DateTime? UpdatedAt`.
2. Tạo tất cả enum files.
3. Tạo entity `Company.cs`, `Meeting.cs`, `MeetingResolution.cs`, `MeetingCandidate.cs` với properties đủ theo schema.
4. Tạo entity `Shareholder.cs` (đủ 16 fields VSDC + meeting_id).
5. Tạo skeleton entities `Proxy.cs`, `Ballot.cs`, `Template.cs`, `AuditLog.cs`.
6. **Ballot.cs**: thêm property `uint xmin` với `[NotMapped]` rồi map trong Configuration (Npgsql hỗ trợ xmin qua `UseXminAsConcurrencyToken()`).

### Bước 2: EF Core + Configurations

1. Cài NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL` v8.x, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
2. `MmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`.
3. Tạo `IEntityTypeConfiguration` cho từng entity — dùng fluent API (không dùng Data Annotations trong Domain).
4. Trong `BallotConfiguration.cs`:
   ```csharp
   builder.UseXminAsConcurrencyToken();
   ```
5. `AuditLogConfiguration.cs`: ghi chú TODO — cần tạo DB trigger `BEFORE UPDATE OR DELETE ON audit_logs RAISE EXCEPTION` sau khi migration apply.

### Bước 3: Initial Migration

1. Từ solution root:
   ```bash
   cd src/Mms.Infrastructure
   dotnet ef migrations add InitialCreate \
     --startup-project ../Mms.Web \
     --output-dir Persistence/Migrations
   ```
2. Review migration file — đảm bảo tất cả bảng + index được tạo.
3. Thêm index thủ công vào migration nếu EF không auto-generate:
   - `CREATE INDEX ON shareholders(meeting_id, id_number)`
   - `CREATE INDEX ON audit_logs(meeting_id, ts DESC)`
   - `CREATE INDEX ON ballots(meeting_id, status)`
   - `CREATE INDEX ON ballots(meeting_id, reprint_needed) WHERE reprint_needed = true`

### Bước 4: ASP.NET Core Identity

1. `ApplicationUser : IdentityUser<Guid>` — thêm `FullName`, `MustChangePassword (bool)`, `LastLoginAt`.
2. `ApplicationRole : IdentityRole<Guid>` — thêm `Description`.
3. Đăng ký trong `Program.cs`:
   ```csharp
   builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(opts => {
       opts.Password.RequiredLength = 8;
       opts.Password.RequireUppercase = true;
       opts.Password.RequireDigit = true;
       opts.Password.RequireNonAlphanumeric = false;
   })
   .AddEntityFrameworkStores<MmsDbContext>()
   .AddDefaultTokenProviders();
   ```

### Bước 5: JWT Token Service

1. Cài `Microsoft.AspNetCore.Authentication.JwtBearer`.
2. `JwtTokenService.cs`:
   - `GenerateAccessToken(ApplicationUser user, IList<string> roles)` → JWT 1h
   - `GenerateRefreshToken()` → random bytes → store in cookie
   - Claims: `sub`, `jti`, `name`, `role`, `must_change_password`
3. `Program.cs`: register `Authentication` + `JwtBearer` + `Authorization` policies.
4. Tạo `AuthController.cs` tại `Mms.Web/Api/`:
   - `POST /api/auth/login` → PasswordSignIn → issue tokens
   - `POST /api/auth/refresh` → validate refresh cookie → new access token
   - `POST /api/auth/logout` → clear cookie
   - `POST /api/auth/change-password` → [Authorize]
   - `POST /api/auth/forgot-password` → (stub: return 200, send email TODO)
   - `POST /api/auth/reset-password` → stub

### Bước 6: Seed Data

1. `SeedData.cs` chạy trong `Program.cs` sau `app.UseAuthentication()`:
   ```csharp
   await SeedData.EnsureSeededAsync(app.Services);
   ```
2. Seed 4 roles: `admin`, `operator`, `viewer`, `checkin`.
3. Seed admin user:
   - Username: `admin` (hoặc đọc từ env `SEED_ADMIN_USERNAME`)
   - Password: `Admin@2026!` (hoặc `SEED_ADMIN_PASSWORD` env) → hash BCrypt
   - `MustChangePassword = true`
   - Role: `admin`
4. Idempotent check: `if (await roleManager.RoleExistsAsync("admin")) return;`

### Bước 7: Serilog Configuration

1. Cài NuGet: `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Sinks.PostgreSQL`.
2. Tạo `SerilogConfiguration.cs`:
   ```csharp
   Log.Logger = new LoggerConfiguration()
       .ReadFrom.Configuration(configuration) // từ appsettings
       .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("logs/mms-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
       .WriteTo.PostgreSQL(connectionString, "logs", needAutoCreateTable: true,
           failureCallback: ex => Console.Error.WriteLine($"Serilog sink error: {ex}"))
       .CreateLogger();
   ```
3. `Program.cs`: `builder.Host.UseSerilog()`.
4. Request logging: `app.UseSerilogRequestLogging()` (bỏ qua health check endpoint).

### Bước 8: App Startup (Program.cs)

1. `builder.Services.AddDbContext<MmsDbContext>()` — connection string từ env `DB_CONNECTION_STRING`.
2. Auto-apply migration on startup (development only, production manual):
   ```csharp
   if (app.Environment.IsDevelopment()) {
       using var scope = app.Services.CreateScope();
       scope.ServiceProvider.GetRequiredService<MmsDbContext>().Database.Migrate();
   }
   ```
3. Docker production: migration chạy qua `dotnet ef database update` trong Dockerfile entrypoint hoặc init container.

### Bước 9: Integration Test

1. Cài `Testcontainers.PostgreSql` vào `Mms.IntegrationTests`.
2. Tạo `DatabaseFixture.cs`: spin up Postgres container → apply migration → seed.
3. Test: `CompanyRepository_CanInsertAndQuery`, `IdentitySeeder_Creates4Roles`, `JwtService_IssuesValidToken`.

### Bước 10: Audit Log Trigger (SQL)

1. Sau khi migration apply, chạy SQL tạo trigger bảo vệ audit_logs:
   ```sql
   CREATE OR REPLACE FUNCTION prevent_audit_log_mutation()
   RETURNS TRIGGER AS $$
   BEGIN
     RAISE EXCEPTION 'audit_logs is append-only: % on audit_logs is not allowed', TG_OP;
   END;
   $$ LANGUAGE plpgsql;

   CREATE TRIGGER audit_log_immutable
   BEFORE UPDATE OR DELETE ON audit_logs
   FOR EACH ROW EXECUTE FUNCTION prevent_audit_log_mutation();
   ```
2. Thêm SQL này vào migration file hoặc chạy trong `SeedData.cs`.

---

## Todo List

- [ ] Cài NuGet packages cho Mms.Domain, Mms.Infrastructure (EF, Npgsql, Identity, Serilog)
- [ ] Tạo BaseEntity, enums (6 files)
- [ ] Tạo tất cả entity files (9 entities)
- [ ] Tạo IEntityTypeConfiguration cho mỗi entity
- [ ] Tạo MmsDbContext : IdentityDbContext
- [ ] Run `dotnet ef migrations add InitialCreate`
- [ ] Review + bổ sung index vào migration
- [ ] Implement ApplicationUser, ApplicationRole
- [ ] Implement JwtTokenService
- [ ] Tạo AuthController (6 endpoints, 2 stub)
- [ ] Implement SeedData (4 roles + 1 admin)
- [ ] Cấu hình Serilog 3 sinks
- [ ] Viết Program.cs DI registration
- [ ] Viết 3 integration tests (Testcontainers)
- [ ] Tạo SQL trigger audit_logs append-only
- [ ] Test docker-compose up → migration tự apply thành công
- [ ] Verify seed data: login admin thành công qua Postman/curl

---

## Success Criteria

- [ ] `dotnet ef database update` / auto-migrate apply thành công tạo đủ bảng.
- [ ] `POST /api/auth/login` với `admin/Admin@2026!` → trả JWT hợp lệ.
- [ ] Decode JWT chứa claim `must_change_password: true` cho admin mới.
- [ ] Serilog ghi vào 3 sink (console hiện log, file tạo, bảng `logs` có row).
- [ ] Integration test xanh 100% (Testcontainers spin up + seed + query OK).
- [ ] Audit log trigger: `UPDATE audit_logs SET ...` → raise exception.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Migration conflict khi nhiều dev | High | Mỗi dev branch riêng; merge migration vào main trước khi tạo migration mới |
| xmin mapping sai (Ballot concurrency) | High | Test Npgsql xmin demo ngay ở phase này — không để phase sau mới phát hiện |
| Serilog Postgres sink exception làm crash app | Medium | `failureCallback` swallow + restart; sink error không throw ra |
| EF lazy loading N+1 queries | Medium | Disable lazy loading global; dùng explicit `.Include()` |
| JWT secret weak hoặc leak | High | Min 32 chars; đọc từ env var; Docker secret trong production |

---

## Security Considerations

- JWT secret: env var `JWT_SECRET` min 32 chars, **không** hardcode, **không** commit vào git.
- DB password: env var `DB_PASSWORD`, không dùng `postgres` làm production password.
- Cookie refresh token: `HttpOnly=true`, `Secure=true` (HTTPS), `SameSite=Strict`.
- Password reset token expiry: 1h.
- Anti-brute-force: `Identity.Lockout.MaxFailedAccessAttempts=5`, `LockoutDuration=15m`.
- HTTPS: self-signed cert trong Docker cho LAN; cấu hình `app.UseHsts()` + `app.UseHttpsRedirection()`.

---

## Next Steps

- Phase-02 bắt đầu được ngay sau khi migration + Auth hoạt động.
- Phase-02 cần: `MmsDbContext` injectable, Identity `SignInManager` + `UserManager`, JWT từ `AuthController`.
- Flag cho Phase-04: bảng `shareholders` đã có đủ columns và UNIQUE index — chỉ cần implement parser + wizard.

---

## Unresolved Questions

1. **Password policy**: yêu cầu non-alphanumeric ký tự đặc biệt không? Expiry policy (30/90 ngày)?
2. **Session timeout**: 8h access + 8h refresh có phù hợp cho ngày đại hội (thường 4-6h)? Nên rút ngắn 4h?
3. **Email forgot-password**: phase này stub — khi nào cần implement SMTP thật? Cần cấu hình SMTP server riêng hay dùng dịch vụ external (SendGrid)?
4. **Audit log trigger**: nên đặt trong EF migration hay seed script? Migration an toàn hơn vì chạy tự động.
