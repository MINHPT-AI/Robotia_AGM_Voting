# BRAINSTORM REPORT — AGM_Voting System Architecture

> **Ngày**: 2026-04-20 15:58 (Asia/Saigon)
> **Tác giả**: Solution Brainstormer
> **Input**: `brd-quy-trinh-dhcd.md` + `ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md`
> **Trạng thái**: Đề xuất chờ duyệt

---

## 1. PROBLEM STATEMENT

Chuyển đổi hệ thống quản lý ĐHCĐ (MMS) từ WinForms hardcoded sang nền tảng Web hiện đại, **chạy on-premise 100% tại máy khách** trên LAN nội bộ, đáp ứng:

- **8 bước quy trình** ĐHCĐ (Thông tin DN → Import VSDC → Thư mời → UQ trước họp → Check-in POS → Thẩm tra → Kiểm phiếu → Report).
- **Template engine trung tâm** (6 loại .docx, đa ngôn ngữ, chỉnh 1 lần, render tự động).
- **Ballot Lifecycle Cascade** (invalidate/regenerate phiếu khi UQ thay đổi, optimistic concurrency trên 5-10 POS song song).
- **POS check-in tốc độ cao** (<1s đọc QR, phím tắt F2/F5/Ctrl+P).
- **Realtime** (SignalR push tỷ lệ tham dự + kết quả lên màn hình chủ tọa + màn chiếu).
- **Chủ quyền dữ liệu tuyệt đối** (0% cloud, không telemetry, không leak sang vendor).
- **Atomic transactions + Audit log** đầy đủ.
- **Phase 2**: Mở rộng E-Voting Online / đăng ký từ xa.

**Quy mô target**: 500-2,000 cổ đông/kỳ, 5-10 bàn POS song song.

---

## 2. EVALUATED APPROACHES

### 2.1 Backend + Frontend Stack

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **.NET 8 + Blazor Server (chọn)** | SignalR native; 1 ngôn ngữ C# full-stack; stateful circuit lý tưởng LAN; Print Agent cũng .NET → share code DTO; free; Docker Linux chạy tốt; dev lead thường có kinh nghiệm từ WinForms cũ | WebSocket phụ thuộc LAN ổn định; memory server mỗi phiên ~2-5MB; SEO n/a (không cần) | ✅ Best fit |
| .NET 8 API + React/Next SPA | UI đẹp, tách frontend/backend rõ | Team phải master 2 stack; double effort; không tận dụng di sản WinForms | ❌ Over-engineered |
| Node.js + Next + Socket.io | Full JS, nhiều thư viện | Team Việt có di sản .NET ít Node senior; deploy Windows phức tạp hơn | ❌ Risk cao |
| Java Spring + React | Enterprise | Overkill + deploy Windows phức tạp | ❌ Không phù hợp |

### 2.2 Database

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **PostgreSQL 16 (chọn)** | Free forever; MVCC tốt cho Optimistic Concurrency; JSONB cho audit; partial index; pg_cron nếu cần; chạy Docker Linux mượt | Team .NET truyền thống quen MSSQL → cần ramp-up Npgsql/EF Provider | ✅ Tiết kiệm license |
| SQL Server Express/Standard | Integration EF Core tốt nhất | Express giới hạn 10GB (vượt sau vài kỳ); Standard ~$3,586/core license → đội giá bán | ❌ Đắt |
| SQLite | 1 file, đơn giản | Write concurrent kém → xung đột Ballot Cascade; không production-grade | ❌ Nguy hiểm |

### 2.3 Template Engine

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **OpenXml + LibreOffice (chọn)** | Free; BRD đã nhắc LibreOffice; bind placeholder trực tiếp `.docx`; chính xác với merge cells; render PDF đồng nhất trên Linux container | LibreOffice process có thể chết, cần supervisor + pool | ✅ Chuẩn mực |
| Aspose.Words / Syncfusion | In-process, không cần sub-process | $2,000-5,000/dev/year license | ❌ Đắt |
| HTML → PDF (Puppeteer) | Thiết kế linh hoạt | Không dùng .docx → user phải học HTML template → FAIL yêu cầu BRD | ❌ Không đáp ứng |

### 2.4 Deployment

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Docker Compose + Windows installer wrapper (chọn)** | Reproducible; update chỉ cần pull image mới; isolated Postgres + LibreOffice + App; dễ offline bundle vào USB | Cần Docker Desktop trên server khách (free với SME) | ✅ |
| IIS + .NET native + Postgres native | Thuần Windows | Môi trường khách không đồng nhất; khó rollback; troubleshoot tốn effort support | ❌ |
| Self-contained .exe | Đơn giản | Không tách service; khó scale SignalR hub | ❌ |

### 2.5 Print Agent

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Windows Service .NET localhost:9100 (chọn)** | Không dialog popup; tốc độ cao; in trực tiếp .pdf/.docx qua Print Spooler API; chạy ngầm; 1 lần cài | Cần cài trên mỗi POS | ✅ |
| window.print() dialog | Không cần cài gì | Dialog chậm, không đáp ứng <1s | ❌ |
| Electron wrapper | Full control | 150MB bundle × N POS | ❌ |

---

## 3. RECOMMENDED SOLUTION — Kiến trúc tổng thể

### 3.1 Stack chốt

| Layer | Công nghệ | Ghi chú |
|-------|----------|---------|
| Backend API + UI | **.NET 8 + Blazor Server** | SignalR tích hợp sẵn, MudBlazor cho component |
| Database | **PostgreSQL 16** + Npgsql + EF Core 8 | MVCC + xtuple optimistic concurrency |
| Realtime | **ASP.NET Core SignalR** | Backplane Redis khi >1 instance (Phase 2) |
| Template render | **DocumentFormat.OpenXml** + **LibreOffice 7.x headless** | Bind placeholder + convert PDF |
| Cache / Queue | **Redis 7** (optional MVP, bắt buộc Phase 2) | Reprint Queue + SignalR backplane |
| Print Agent | **.NET 8 Worker Service self-contained** (Windows Service) | HTTP localhost:9100, Swagger ẩn |
| Auth | **ASP.NET Core Identity** + JWT + BCrypt | Role: admin/operator/viewer/checkin |
| Barcode/QR | **ZXing.Net** + **QRCoder** | Server-side generate |
| File parsing VSDC | **EPPlus** (non-commercial license OK) hoặc **ClosedXML** | Đọc cứng 16 cột, merge header |
| Logging | **Serilog** → file + Postgres sink | Audit log riêng table |
| Packaging | **Docker Compose** + **Inno Setup** (Windows installer) | `.exe` 1-click |
| CI/CD nội bộ | **GitHub Actions self-hosted** hoặc Jenkins local | Build image → push local registry |

**Chi phí license: $0.** Toàn bộ stack open-source / free.

### 3.2 Architecture Diagram

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                       KHÁCH HÀNG (On-Premise LAN)                        │
│                                                                          │
│   ┌─────────────────────────── SERVER TRUNG TÂM ─────────────────────┐  │
│   │  (Windows 10/11 Pro / Server 2019+, Docker Desktop)              │  │
│   │                                                                   │  │
│   │   docker-compose:                                                 │  │
│   │   ┌──────────────────┐  ┌──────────────────┐                    │  │
│   │   │ blazor-app (.NET)│◄─┤ signalr-hub      │                    │  │
│   │   │ Port 8080        │  │ (in-process)     │                    │  │
│   │   └────────┬─────────┘  └──────────────────┘                    │  │
│   │            │                                                      │  │
│   │   ┌────────▼─────────┐  ┌──────────────────┐                    │  │
│   │   │ postgres:16      │  │ libreoffice-svc  │                    │  │
│   │   │ Port 5432 (local)│  │ (convert-pdf)    │                    │  │
│   │   └──────────────────┘  └──────────────────┘                    │  │
│   │                                                                   │  │
│   │   ┌──────────────────┐  Volumes:                                 │  │
│   │   │ redis:7          │  • /data/postgres                         │  │
│   │   │ (optional Phase1)│  • /data/templates (*.docx)               │  │
│   │   └──────────────────┘  • /data/scans (UQ scan)                  │  │
│   │                          • /data/ballots (PDF rendered)          │  │
│   └──────────┬──────────────────────────────────────────────────────┘  │
│              │ LAN switch                                               │
│              │                                                          │
│   ┌──────────┴──────────┬────────────────┬────────────────────────┐    │
│   ▼                     ▼                ▼                        ▼    │
│ ┌─────────┐        ┌─────────┐      ┌─────────┐            ┌─────────┐ │
│ │POS #1   │        │POS #2   │      │POS #N   │            │Màn chiếu│ │
│ │(Browser │        │         │ ...  │         │            │ (Kiosk) │ │
│ │ Chrome/ │        │         │      │         │            │         │ │
│ │  Edge)  │        │         │      │         │            └─────────┘ │
│ │+Print   │        │+Print   │      │+Print   │                        │
│ │ Agent   │        │ Agent   │      │ Agent   │            ┌─────────┐ │
│ │ :9100   │        │ :9100   │      │ :9100   │            │Chủ tọa  │ │
│ └────┬────┘        └────┬────┘      └────┬────┘            │(Laptop) │ │
│      │ USB printer      │                 │                  └─────────┘ │
│      ▼                  ▼                 ▼                              │
│ ┌────────┐         ┌────────┐        ┌────────┐                         │
│ │Máy in  │         │Máy in  │        │Máy in  │                         │
│ │A5/A4   │         │A5/A4   │        │A5/A4   │                         │
│ └────────┘         └────────┘        └────────┘                         │
└──────────────────────────────────────────────────────────────────────────┘
         ✂ Không kết nối Internet bên ngoài (Air-gap optional)
```

### 3.3 Phân lớp code (Clean Architecture)

```
src/
├── Mms.Domain/              # Entity + Value Object + Domain Event (no dependency)
│   ├── Aggregates/
│   │   ├── Meeting/
│   │   ├── Shareholder/
│   │   ├── Proxy/
│   │   ├── Ballot/          ⭐ Ballot Lifecycle root
│   │   ├── Template/
│   │   └── AuditLog/
│   └── Services/
│       └── BallotLifecycleService.cs  ⭐ Cascade logic
├── Mms.Application/          # Use case + CQRS (MediatR)
│   ├── Meetings/Commands, Queries
│   ├── Proxies/
│   ├── CheckIns/
│   ├── Ballots/
│   └── Templates/
├── Mms.Infrastructure/       # EF Core, Postgres, Redis, LibreOffice, File IO
│   ├── Persistence/
│   ├── Print/                # Gọi Print Agent HTTP
│   ├── Templating/           # OpenXml + LibreOffice pool
│   └── Realtime/             # SignalR Hubs
├── Mms.Web/                  # Blazor Server + MudBlazor
│   ├── Pages/ (30 MH theo UI spec)
│   ├── Components/
│   └── Hubs/
│       ├── CheckInHub.cs
│       ├── BallotHub.cs
│       └── DisplayHub.cs
├── Mms.PrintAgent/           # Windows Service riêng
└── Mms.Tests/
    ├── Unit/
    ├── Integration/ (Testcontainers Postgres)
    └── E2E/ (Playwright)
```

### 3.4 Database Schema (highlights)

```sql
-- Core entities
CREATE TABLE companies (id UUID PK, name TEXT, tax_code TEXT, charter_capital BIGINT, total_voting_shares BIGINT, ...);
CREATE TABLE meetings (id UUID PK, company_id FK, title TEXT, type meeting_type, status meeting_status,
                       record_date DATE, meeting_date TIMESTAMPTZ, ...);
CREATE TABLE resolutions (id UUID PK, meeting_id FK, display_order INT, title TEXT, content TEXT);
CREATE TABLE candidates (id UUID PK, meeting_id FK, position candidate_position, display_order INT, full_name TEXT);

-- Shareholders từ VSDC (cột 1-16 giữ nguyên)
CREATE TABLE shareholders (
  id UUID PK,
  meeting_id FK,
  vsdc_row INT,
  full_name TEXT NOT NULL,
  sid TEXT,
  investor_code TEXT,
  id_number TEXT NOT NULL,          -- Cột 5
  id_issue_date DATE,
  address TEXT,
  email TEXT,
  phone TEXT,
  nationality TEXT,                 -- Cột 10
  shares_non_deposit BIGINT,
  shares_deposit BIGINT,
  shares_total BIGINT,
  rights_non_deposit BIGINT,
  rights_deposit BIGINT,
  voting_rights BIGINT NOT NULL,    -- Cột 16 — MANDATORY
  UNIQUE(meeting_id, id_number)
);

-- Proxy (ủy quyền)
CREATE TABLE proxies (
  id UUID PK,
  meeting_id FK,
  grantor_shareholder_id FK,        -- người ủy quyền
  grantee_name TEXT,
  grantee_id_number TEXT,
  shares BIGINT,
  scope proxy_scope,                -- FULL/PARTIAL
  proxy_type proxy_type,            -- PRE_MEETING / ON_SITE
  proxy_date DATE,
  scan_url TEXT,
  created_at TIMESTAMPTZ,
  invalidated_at TIMESTAMPTZ,
  CHECK (shares > 0)
);

-- Ballot Lifecycle ⭐
CREATE TABLE ballots (
  id UUID PK,
  meeting_id FK,
  shareholder_id FK,                -- chủ phiếu hiện tại (người cầm thẻ)
  attend_code TEXT UNIQUE,          -- Mã tham dự auto-gen
  voting_shares BIGINT,
  direct_shares BIGINT,             -- CP sở hữu tham dự trực tiếp
  proxy_shares BIGINT,              -- CP nhận UQ
  status ballot_status,             -- ACTIVE / INVALIDATED / REGENERATED
  parent_ballot_id FK NULL,         -- trace lại phiếu cũ bị hủy
  reprint_needed BOOLEAN DEFAULT false,
  invalidation_reason TEXT,
  pos_terminal TEXT,                -- bàn nào in
  operator_user_id FK,
  created_at TIMESTAMPTZ,
  invalidated_at TIMESTAMPTZ,
  printed_at TIMESTAMPTZ,
  row_version BYTEA,                -- xmin of Postgres → optimistic concurrency
  INDEX (meeting_id, status),
  INDEX (meeting_id, reprint_needed) WHERE reprint_needed = true
);

-- Vote records
CREATE TABLE vote_records (
  id UUID PK,
  ballot_id FK,
  resolution_id FK NULL,            -- nullable vì có thể là bầu cử
  candidate_id FK NULL,
  choice vote_choice,               -- AGREE / DISAGREE / ABSTAIN / INVALID
  shares_voted BIGINT,              -- cho bầu dồn phiếu
  is_valid BOOLEAN,
  recorded_by FK,
  recorded_at TIMESTAMPTZ
);

-- Templates
CREATE TABLE templates (
  id UUID PK,
  meeting_id FK,
  template_type template_type,       -- INVITATION, VOTING_CARD, VOTING_BALLOT, ELECTION_BALLOT, ATTENDANCE_REPORT, COUNTING_REPORT
  language template_language,        -- VN/EN/DUAL
  version INT,
  file_path TEXT,
  fields_config JSONB,               -- selected fields
  is_finalized BOOLEAN,
  uploaded_by FK,
  uploaded_at TIMESTAMPTZ,
  UNIQUE(meeting_id, template_type, language, version)
);

-- Audit log (append-only)
CREATE TABLE audit_logs (
  id BIGSERIAL PK,
  ts TIMESTAMPTZ DEFAULT now(),
  user_id FK NULL,
  actor TEXT,                        -- system / user / cascade
  category audit_category,           -- CheckIn, Proxy, Ballot, CASCADE, Print, Report, Auth
  entity_type TEXT,
  entity_id UUID,
  meeting_id UUID,
  detail JSONB,
  pos_terminal TEXT
);
CREATE INDEX ON audit_logs (meeting_id, ts DESC);
CREATE INDEX ON audit_logs (category, ts DESC);
```

### 3.5 BALLOT LIFECYCLE CASCADE (critical design)

```text
Event: "Check-in lần đầu" → BallotService.CreateForCheckIn(shareholder, proxies)
  BEGIN TXN
    1. INSERT ballot (status=ACTIVE, voting_shares=direct+proxy)
    2. INSERT vote_records (rỗng ban đầu)
    3. INSERT audit_log (category=Ballot, action=CREATE)
  COMMIT → notify CheckInHub → update live dashboard

Event: "UQ mới tại chỗ" → BallotService.CascadeNewProxy(grantor, grantee, shares)
  BEGIN TXN SERIALIZABLE
    1. Kiểm tra CP còn lại grantor (SELECT FOR UPDATE)
    2. INSERT proxy (type=ON_SITE)
    3. IF grantor.ballot.status = ACTIVE:
         UPDATE grantor.ballot SET status=INVALIDATED, invalidated_at=now(), reprint_needed=true
         INSERT new ballot (parent=grantor.ballot, status=ACTIVE, voting_shares giảm)
    4. IF grantee.ballot.status = ACTIVE:
         UPDATE grantee.ballot SET status=INVALIDATED, reprint_needed=true
         INSERT new ballot (parent=grantee.ballot, status=ACTIVE, voting_shares tăng)
    5. INSERT audit_log cascade entries
  COMMIT → notify ReprintQueueHub + DisplayHub

Concurrency: row_version check + Postgres SERIALIZABLE hoặc SELECT FOR UPDATE NOWAIT
              → nếu xung đột → retry 3 lần với exponential backoff (Polly)
```

**Rule bất biến**:
- Phiếu `INVALIDATED` KHÔNG xóa khỏi DB → audit trail đầy đủ.
- Chỉ phiếu `ACTIVE` mới được tính vote.
- `reprint_needed` cờ để UI Reprint Queue.
- Mỗi lần regenerate → tạo row mới + link `parent_ballot_id`.

### 3.6 Template Engine Flow

```text
1. User upload .docx tại D2 UI
   → Lưu /data/templates/{meeting_id}/{type}/{language}/v{N}.docx
   → Scan placeholder {{...}} → gợi ý fields list

2. User chọn fields + bấm "Chốt template"
   → Lưu fields_config JSONB + is_finalized=true

3. Khi in phiếu (ví dụ F5 Check-in):
   a. TemplateRenderer.Render(ballot_id)
      → Load .docx bytes
      → OpenXml replace {{placeholder}} bằng data ballot (per-CĐ)
      → Nếu Phiếu BQ (loại 3): inject table DS nội dung BQ
      → Save tmp .docx
   b. LibreOfficePool.ConvertToPdf(tmp.docx)
      → spawn `soffice --headless --convert-to pdf --outdir /tmp tmp.docx`
      → timeout 5s, pool size 3, restart on crash
   c. PrintService.SendToAgent(pdf_bytes, pos_terminal)
      → HTTP POST localhost:9100/print body = PDF base64
      → Agent gọi Windows Print Spooler → máy in
   d. UPDATE ballot SET printed_at = now()
```

**Tối ưu**:
- Pre-warm LibreOffice pool khi app khởi động (5-10s đầu chậm, sau đó ổn).
- Cache template parsed (OpenXml tree) trong memory.
- Batch render (Thư mời 1,000 phiếu) → multi-thread với semaphore.

### 3.7 Realtime SignalR Architecture

**3 Hubs riêng biệt** (tránh 1 hub to đùng):

| Hub | Tương tác | Ai subscribe |
|-----|-----------|--------------|
| `CheckInHub` | `AttendanceUpdated(count, shares, ratio)` | Dashboard chủ tọa, DS tham dự live (G4), Header POS |
| `BallotHub` | `ReprintQueued(ballots)`, `BallotCascaded(details)` | Panel Reprint Queue (G3), Audit log view |
| `DisplayHub` | `ResolutionPublished(result)`, `ElectionPublished(result)` | Màn chiếu K1 (kiosk mode) |

**Groups**: theo `meeting_id` để không broadcast chéo khi có nhiều meeting (trong testing).

**Reconnect**: Blazor Server tự retry 3 lần với backoff; nếu fail → hiển thị banner vàng.

### 3.8 Print Agent (Windows Service)

```
Mms.PrintAgent/
├── Worker.cs            # BackgroundService
├── HttpApi/
│   ├── PrintController  # POST /print, GET /printers, POST /test
│   └── Auth.cs          # Token-based, chia sẻ với main app qua config
└── Printing/
    └── SpoolerService.cs  # System.Drawing.Printing + System.Printing
```

- Self-contained single-file .exe (~60MB).
- Cài qua `sc.exe create MmsPrintAgent binPath="..."`.
- Installer setup firewall rule localhost-only.
- Log rotate daily Serilog.

---

## 4. NON-FUNCTIONAL REQUIREMENTS

| NFR | Target | Cách đạt |
|-----|--------|---------|
| **Latency POS** | <1s quét QR → hiển thị CĐ | Blazor Server LAN + index on id_number + memory cache hot data |
| **Throughput** | 10 bàn × 6 CĐ/phút = 60 checkin/min peak | EF bulk + row-level lock, connection pool 20-30 |
| **Availability** | 99% trong 4h đại hội | Auto-restart docker container; HA optional Phase 2 |
| **Data integrity** | 0 phiếu mồ côi | SERIALIZABLE transaction cho Ballot Cascade + constraint DB |
| **Backup** | RTO 10 phút, RPO 1 phút | pg_dump mỗi 1 phút → NAS/USB; manual snapshot trước và sau đại hội |
| **Security** | JWT + RBAC + HTTPS LAN self-signed | Cert trust tự động qua installer |
| **Audit** | Full trail không sửa được | Append-only table, trigger chặn UPDATE/DELETE |
| **Offline** | 100% LAN, không Internet | Docker images bundle USB; package offline |
| **Multilingual print** | Auto detect nationality | Column 10 → enum → chọn template language |
| **Print speed** | <2s in 1 phiếu | LibreOffice pool warm + Print Agent native spooler |

---

## 5. RISKS + MITIGATIONS

| # | Risk | Mức độ | Mitigation |
|---|------|--------|-----------|
| R1 | **LibreOffice headless crash** giữa đại hội → không in được phiếu | 🔴 Cao | Pool 3 instances + health check + auto-restart; fallback: Print Agent render trực tiếp .docx khi LibreOffice down (chất lượng thấp hơn nhưng vẫn in được) |
| R2 | **Ballot Cascade deadlock** khi 2 POS xử lý UQ chéo cùng lúc | 🔴 Cao | SERIALIZABLE + retry Polly 3 lần; nếu vẫn fail → báo operator chờ; log chi tiết |
| R3 | **Blazor Server WebSocket drop** khi mất LAN | 🟡 Vừa | Auto-reconnect + local state buffer (IndexedDB); banner cảnh báo; server timeout nới 10 phút |
| R4 | **VSDC file format thay đổi** (VSDC upgrade) | 🟡 Vừa | Parser defensive, kiểm tra 16 cột tên chính xác; fallback manual mapping "Emergency Mode" flag admin |
| R5 | **Print Agent bị antivirus block** trên máy khách | 🟡 Vừa | Code-sign .exe với cert (~$200/năm); docs cài whitelist; test trên Defender/Kaspersky |
| R6 | **PostgreSQL disk full** sau nhiều kỳ đại hội | 🟢 Thấp | Archive meeting cũ vào separate DB; alert disk >80% |
| R7 | **Team chưa quen Blazor Server** → velocity chậm | 🟡 Vừa | Training 1 tuần + pair programming 2 tuần đầu; MudBlazor giảm learning curve |
| R8 | **Customer không có Docker Desktop** (enterprise lock) | 🟡 Vừa | Cung cấp 2 bản: (a) Docker (ưu tiên), (b) Native MSI IIS + Postgres standalone |
| R9 | **QR/Barcode reader tương thích** (mã POS cũ) | 🟢 Thấp | Standard USB HID keyboard emulation → tương thích 99% máy thị trường |
| R10 | **Cascade regenerate infinite loop** khi logic buggy | 🔴 Cao | Depth limit 10 + circuit breaker + dedicated integration test suite |

---

## 6. SUCCESS METRICS

| Metric | Baseline | Target MVP-Core |
|--------|---------|----------------|
| Thời gian check-in 1 CĐ | 45-60s (WinForms cũ) | <15s (POS + QR) |
| Thời gian kết thúc check-in 1,000 CĐ (10 bàn) | 2h+ | 30-40 phút |
| Tỷ lệ phiếu invalid/reprint | Manual, >5% | <1% |
| Thời gian render BB kiểm phiếu | Manual nửa ngày | <30s |
| Uptime trong đại hội | Target 99.9% 4h | Same |
| Số bug blocker tại đại hội thực | — | 0 |
| Số user onboarding < 2h train | — | ≥80% staff |

---

## 7. TIMELINE ĐỀ XUẤT (Realistic — 7 tháng, 5 dev)

> User để tôi đề xuất. Đây là kế hoạch conservative-realistic.

| Giai đoạn | Thời gian | Deliverable |
|-----------|-----------|-------------|
| **Sprint 0 — Foundation** | 3 tuần | Setup repo, Docker Compose, EF migrations, Auth, 1 MH smoke test |
| **Sprint 1 — Meeting + VSDC Import** | 3 tuần | B1/B2, C1/C2 (Wizard 4 bước import), unit test parser |
| **Sprint 2 — Template Engine** | 4 tuần | D1/D2, OpenXml + LibreOffice pool, 6 loại template, multilingual |
| **Sprint 3 — Proxy + Invitation** | 2 tuần | E1, F1/F2 |
| **Sprint 4 — Check-in POS + Ballot Lifecycle** | 5 tuần | G1/G2/G3/G4, BallotLifecycleService, Print Agent, Cascade integration test |
| **Sprint 5 — Tallying + Reports** | 4 tuần | H1, I1-I5, J1/J2, Consistency Check |
| **Sprint 6 — Display + Admin** | 2 tuần | K1/K2, L1/L2/L3, Audit log query UI |
| **Sprint 7 — UAT + Load Test + Hardening** | 3 tuần | Load test k6/NBomber (60 checkin/min), Playwright E2E 8 quy trình, installer Inno Setup, docs |
| **Buffer** | 2 tuần | Bug fix, customer pilot |

**Tổng: 28 tuần ≈ 7 tháng.**

**Team đề xuất**:
- 1 Tech Lead (Blazor + Postgres)
- 2 Full-stack .NET dev
- 1 Frontend + UX focus (MudBlazor)
- 1 QA Automation (Playwright + k6)
- 0.5 DevOps (part-time setup Docker + installer)
- 0.5 BA/PM (part-time reqs + UAT)

---

## 8. PHASE 2 PREPARATION (E-Voting Online)

Trong MVP-Core đã **reserve** các điều kiện để Phase 2 không phải refactor lớn:

1. **Multi-tenancy ready**: Entity đã có `company_id` → switch được.
2. **Auth abstract**: Identity provider interface → dễ plug SSO/OTP SMS/Zalo.
3. **Ballot abstraction**: `ballot.channel` enum (POS / WEB / MOBILE) thêm sau.
4. **SignalR backplane**: Từ in-process sang Redis khi scale out.
5. **API-first**: Tất cả business logic qua API endpoint → mobile/web app sau dùng cùng API.
6. **Event Sourcing optional**: Audit log hiện tại đã "event-like" → dễ migrate.

---

## 9. ADDITIONAL REQUIREMENTS NEEDED

Để project chạy thành công, cần chuẩn bị thêm:

### 9.1 Vận hành / Tổ chức
- **Training plan**: 2 ngày cho operator check-in; 1 ngày cho admin.
- **Runbook**: tài liệu xử lý sự cố khi đại hội (LibreOffice crash, reprint lỗi, khôi phục sau mất điện).
- **Data migration** từ WinForms cũ (nếu có): script ETL, map Schema cũ → mới.
- **Pilot customer**: 1 công ty đại chúng vừa (500-1000 CĐ) chạy thử đại hội thực tế.

### 9.2 Compliance
- **Pháp lý**: chứng nhận đáp ứng Luật Doanh nghiệp 2020 về ĐHCĐ; Nghị định 155/2020 + 53/2025 (nếu mới).
- **UBCKNN**: format báo cáo snapshot DS cổ đông tham dự phải match yêu cầu Ủy ban Chứng khoán Nhà nước.
- **Kiểm toán**: audit log phải chống sửa; hash chain optional.

### 9.3 Hardware
- **Server**: CPU 8-core, RAM 16GB, SSD 500GB, Windows 10/11 Pro hoặc Server 2019+.
- **POS**: Laptop/PC modest, Chrome/Edge mới, máy in nhiệt A5 + máy in A4 laser.
- **Scanner**: USB QR/Barcode reader HID keyboard.
- **Display**: TV/projector hội trường + 1 laptop riêng chủ tọa.
- **Network**: LAN switch gigabit, WiFi backup, UPS cho server.

### 9.4 Branding / UX
- **Design system**: Palette + typography Vietnamese công ty đại chúng (formal, serif cho báo cáo).
- **Keyboard shortcut sticker**: dán trên bàn POS cho staff.
- **POS workflow poster**: A3 print quầy check-in cho cổ đông hiểu.

---

## 10. VALIDATION CRITERIA (Definition of Done MVP-Core)

- [ ] Chạy đại hội mô phỏng 1,000 CĐ ảo trên 5 POS trong 40 phút không crash.
- [ ] Ballot Cascade pass 100% scenario test (50 kịch bản UQ chéo).
- [ ] Template render 6 loại đúng mẫu PDF/DOCX reference.
- [ ] Multi-lang auto detect nationality correct 100%.
- [ ] Consistency Check SUM ballot = SUM CP tham dự luôn khớp.
- [ ] Audit log ghi đầy đủ 100% thao tác quan trọng.
- [ ] Installer 1-click cài được trên Windows 10/11 pristine.
- [ ] E2E Playwright cover 8 bước quy trình.
- [ ] Load test 60 checkin/min sustained 10 phút p95 < 800ms.
- [ ] Pilot 1 công ty thực tế đại hội success.

---

## 11. NEXT STEPS

1. **User approve report này** → thống nhất kiến trúc.
2. **Confirm timeline 7 tháng** hoặc điều chỉnh.
3. **Gen detailed implementation plan** qua `/plan` command với context report này → tạo thư mục `plans/260420-1558-agm-voting-mvp-core/` với phases:
   - phase-00-foundation.md
   - phase-01-meeting-vsdc.md
   - phase-02-template-engine.md
   - phase-03-proxy-invitation.md
   - phase-04-checkin-pos.md
   - phase-05-ballot-lifecycle.md (critical)
   - phase-06-tallying-reports.md
   - phase-07-display-admin.md
   - phase-08-hardening-uat.md

---

## 12. UNRESOLVED QUESTIONS

1. ❓ **Pilot customer**: có khách hàng nào cam kết thử pilot chưa? Ảnh hưởng timeline.
2. ❓ **Di sản WinForms**: có DB hoặc config cần migrate không? Nếu có, cần +2 tuần viết ETL.
3. ❓ **Phase 2 E-Voting**: có bị ràng buộc deadline pháp lý nào (UBCKNN e-voting mandate) không?
4. ❓ **Code-signing cert** cho installer + Print Agent: ngân sách có support $200-500/năm không?
5. ❓ **Máy in mẫu thử nghiệm**: cần chuẩn loại nào? Epson TM-T88, HP LaserJet, Zebra? → ảnh hưởng driver Print Agent.
6. ❓ **Ngôn ngữ backup template**: BRD nhắc Tiếng Anh + Song ngữ. Có cần thêm ngôn ngữ khác (TQ, Nhật, Hàn cho CĐ nước ngoài)?
7. ❓ **Ngân sách hardware server**: khách tự mua hay gói kèm giải pháp?
8. ❓ **SLA hỗ trợ**: on-site trong đại hội thực tế hay remote support đủ? Nếu on-site → cần kỹ sư di chuyển.

---

**End of report.**
