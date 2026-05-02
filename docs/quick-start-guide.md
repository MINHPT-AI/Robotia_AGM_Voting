# 🚀 Quick Start Guide — Robotia AGM Voting System

## Prerequisites

| Tool | Version | Check Command |
|------|---------|--------------|
| Docker Desktop | 4.x+ | `docker --version` |
| .NET SDK | 8.0.x | `dotnet --version` |
| Node.js (optional, for Playwright) | 18+ | `node --version` |

---

## 1️⃣ Clone & Build

```bash
git clone https://github.com/your-org/robotia-agm-voting.git
cd robotia-agm-voting
dotnet restore
dotnet build --configuration Release
```

## 2️⃣ Run with Docker Compose

```bash
# Start all services (app + PostgreSQL)
docker compose -f docker/docker-compose.yml up -d --build

# Verify services are running
docker compose -f docker/docker-compose.yml ps
```

> [!NOTE]
> The app will be available at **http://localhost:8080** after about 30 seconds.

## 3️⃣ Default Login

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@123` |

> [!IMPORTANT]
> You will be prompted to change the password on first login.

## 4️⃣ Run Tests

### Unit Tests (no Docker required)
```bash
dotnet test tests/Mms.UnitTests/ --configuration Release
```

### Integration Tests (Docker required for Testcontainers)
```bash
dotnet test tests/Mms.IntegrationTests/ --configuration Release
```

### E2E Tests (Docker Compose must be running)
```bash
# Install Playwright browsers first
pwsh tests/Mms.E2ETests/bin/Release/net8.0/playwright.ps1 install chromium

# Run E2E tests
MMS_E2E_URL=http://localhost:8080 dotnet test tests/Mms.E2ETests/ --configuration Release
```

## 5️⃣ Stop Services

```bash
docker compose -f docker/docker-compose.yml down -v
```

---

## Architecture Overview

```
┌────────────────────────────────────────────────────┐
│              Blazor Server (MudBlazor)             │
│                    :8080                           │
├────────────────────────────────────────────────────┤
│  Application Layer    │  Infrastructure Layer      │
│  (MediatR + FV)       │  (EF Core + Npgsql)       │
├────────────────────────────────────────────────────┤
│  Domain Layer (Entities + Enums)                   │
├────────────────────────────────────────────────────┤
│  PostgreSQL 16 (docker)                            │
└────────────────────────────────────────────────────┘
```

## Test Pyramid

| Layer | Project | Count | Docker? |
|-------|---------|-------|---------|
| Unit | `Mms.UnitTests` | 39 | ❌ |
| Integration | `Mms.IntegrationTests` | 11 | ✅ (Testcontainers) |
| E2E | `Mms.E2ETests` | 4 | ✅ (docker-compose) |
| **Total** | | **54** | |

## Performance Gate

| Benchmark | Target | Actual |
|-----------|--------|--------|
| Import 1,000 VSDC rows | < 10 seconds | **~2 seconds** ✅ |
