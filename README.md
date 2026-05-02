# MMS (Meeting Management System) - AGM Voting

## Prerequisites
- Windows 10/11
- .NET 8 SDK
- Docker Desktop (WSL2 or Hyper-V)

## Quick Start

1. Create environment file:
   ```bash
   cp .env.example .env
   ```

2. Start services (Postgres, Web App, LibreOffice Stub):
   ```bash
   docker-compose up -d --build
   ```

3. Open your browser and navigate to:
   [http://localhost:8080](http://localhost:8080)

## Project Structure (Clean Architecture)
- `Mms.Domain`: Core entities and interfaces
- `Mms.Application`: Use cases, CQRS (MediatR), Validation
- `Mms.Infrastructure`: EF Core, Postgres integrations
- `Mms.Web`: Blazor Web App (Interactive Server)
- `Mms.PrintAgent`: Background worker stub
- `tests/*`: Unit, Integration, and E2E Tests

## Troubleshooting
- **Port Conflict (8080 / 5432)**: Modify `docker-compose.yml` to map to a different host port (e.g., `8081:8080`).
- **WSL2 Memory Issue**: Create a `.wslconfig` file in your Windows User folder to limit WSL memory usage.
