---
title: "AGM Voting MVP-Core — Pilot Foundation + VSDC Import"
description: "6-week pilot validating .NET 8 Blazor Server + Postgres stack với Foundation (Docker, Auth, UI skeleton) + Meeting CRUD + VSDC Import Wizard"
status: done
priority: P1
effort: 6w
branch: main
tags: [agm, blazor, postgres, pilot, foundation, vsdc-import]
created: 2026-04-20
---

# PILOT 6-Week — Foundation + VSDC Import

## Brief

Pilot 6 tuần validate toàn bộ stack on-premise (.NET 8 + Blazor Server + Postgres 16 + Docker Compose) trước khi commit full roadmap 7 tháng. Scope: Foundation infra + Auth + Meeting CRUD + VSDC Parser 16 cột + Import Wizard 4 bước. KHÔNG bao gồm Template Engine, Ballot Cascade, POS Check-in, Tallying.

## Context Links

- Brainstorm: [`plans/reports/brainstorm-260420-1558-agm-voting-system-architecture.md`](../reports/brainstorm-260420-1558-agm-voting-system-architecture.md)
- BRD: [`brd-quy-trinh-dhcd.md`](../../brd-quy-trinh-dhcd.md) (Bước 1, Bước 2)
- UI Spec: [`ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md`](../../ui-screens-specification-mvp-core-va-mvp-full-260420-0110.md) (Section A, B, C)
- VSDC sample: `ExempleTemplate_file/Mẫu file DS VSDC gui.xlsx` + `.csv`

## AI Context Notes

> **Quan trọng cho AI Agents & Lập trình viên:** 
> Để hiểu nhanh bối cảnh code, luồng logic chính, và các nguyên tắc/khắc phục lỗi của từng Phase đã triển khai mà không cần đọc lại toàn bộ codebase, **hãy luôn đọc file này đầu tiên**:
> 👉 [`../../docs/context_style_notes.md`](../../docs/context_style_notes.md)

## Stack (CHỐT — không thảo luận lại)

- .NET 8 + Blazor Server + MudBlazor + SignalR (in-process)
- PostgreSQL 16 + EF Core 8 + Npgsql
- Docker Compose (3 services: blazor-app, postgres, libreoffice stub)
- ASP.NET Core Identity + JWT + BCrypt
- Serilog → console + rolling file + Postgres sink
- Excel parser: **ClosedXML** (MIT) — xem unresolved Q1
- xUnit + FluentAssertions + Testcontainers Postgres
- Playwright .NET cho E2E
- Clean Architecture 6 projects (Domain → Application → Infrastructure → Web + PrintAgent stub + Tests)

## Team & Timeline

| Role | FTE | Trách nhiệm |
|------|-----|-------------|
| Tech Lead | 1 | Blazor + Postgres architecture, code review |
| Full-stack .NET | 2 | Implement |
| QA Automation | 0.5 | Playwright + unit test coverage |

**Timeline: 6 tuần = Sprint 0 Foundation (T1-T3) + Sprint 1 Meeting+VSDC (T4-T6)**

| Tuần | Phase |
|------|-------|
| 1 | phase-00 Repo/Solution/Docker |
| 2 | phase-01 Database + Auth/Identity |
| 3 | phase-02 Foundation UI + Login + Dashboard |
| 4 | phase-03 Company + Meeting CRUD |
| 5 | phase-04 VSDC Parser + Import Wizard |
| 6 | phase-05 Testing + Hardening + Demo |

## Phases

- [x] [Phase 00 — Repo + Solution + Docker Compose Setup](./phase-00-repo-solution-setup-docker-compose.md) *(done)*
- [x] [Phase 01 — Database + Auth/Identity](./phase-01-database-auth-identity.md) *(done)*
- [x] [Phase 02 — Blazor Server + MudBlazor: Login, Dashboard UI & CI/CD](./phase-02-blazor-server-mudblazor-login-dashboard-ui.md) *(done)*
- [x] [Phase 03 — Company Info (mở rộng Thông tin, Mã CK, Logo/Chữ ký) + Tách bảng HĐQT/BKS cho Meeting CRUD](./phase-03-company-info-meeting-crud-resolutions-candidates.md) *(done)*
- [x] [Phase 04 — VSDC Excel Parser (16 Fixed Columns) + Import Wizard + Shareholder Upsert](./phase-04-vsdc-excel-parser-16-columns-import-wizard-shareholder-upsert.md) *(done)*
- [x] [Phase 05 — Unit + Integration + E2E Playwright Tests, Performance Hardening & Pilot Demo](./phase-05-unit-integration-e2e-playwright-tests-performance-demo.md) *(done — 50 tests green, 1000 rows ~4s)*
- [x] [Phase 06A — Gửi Thư Mời Giấy (Physical Invitation Letter Management)](../reports/phase-06a-invitation-letters-physical-mailing-implementation-plan.md) *(done — Domain + Infra + App + UI 3-tab)*

### Expansion Scope (Added post-pilot)
- [x] Phase 08 — Quản lý Ủy quyền (Proxy Management) *(done — Domain extensions, UI 2-column, Business Rules)*
- [x] Phase 09 — Bàn Check-in & Thẩm tra tư cách (Check-in Workbench & Attendance) *(done — Atomic Check-in, SignalR sync, Quorum snapshots)*

## Exit Criteria Pilot ✅ ALL MET

1. ✅ `docker-compose up` thành công trên Windows fresh (Docker Desktop)
2. ✅ Login admin → Dashboard B1 load OK
3. ✅ Tạo meeting + import 1,000 CĐ **~4s** (target < 10s)
4. ✅ Playwright E2E scaffolded (4 scenarios, build clean) — chạy khi docker-compose up
5. ⏳ Team confident với stack → approve plan full 7 tháng (chờ demo)

## Unresolved Questions

1. **EPPlus v5+ commercial** vs **ClosedXML MIT** — khuyến nghị ClosedXML tránh license rủi ro. Team confirm?
2. File VSDC sample có khớp đúng 16 cột format khách hàng thực tế? Cần test với file production thật.
3. Password policy cụ thể (length, complexity, expiry) — Product confirm.
4. Session/cookie timeout policy — mặc định 8h có ổn không?
5. GitHub Actions runner — Windows self-hosted vs Linux hosted (chi phí vs tốc độ)?
6. Code-sign cert cho installer — chưa cần sprint này, nhưng cần budget sớm ($200-500/năm).
