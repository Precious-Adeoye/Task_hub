# TaskHub

A secure, multi-tenant todo management platform built with .NET 9 and React. Demonstrates production-ready practices including Clean Architecture, RBAC, audit logging, optimistic concurrency, and switchable storage backends.

## Features

- **Authentication** — Cookie-based sessions with BCrypt hashing, account lockout, and IP rate limiting
- **Multi-Tenancy** — Organisation-scoped data isolation with `X-Organisation-Id` header
- **RBAC** — Member and OrgAdmin roles with custom authorization attributes
- **Todo Management** — Full CRUD with soft delete, restore, hard delete, status toggle, filtering, sorting, pagination
- **Optimistic Concurrency** — ETag/If-Match with HTTP 412 on conflict
- **Import/Export** — JSON and CSV with idempotent import (ClientProvidedId)
- **Audit Logging** — All mutations tracked with correlation IDs
- **Validation** — FluentValidation on all inputs with RFC 7807 ProblemDetails errors
- **Dual Storage** — InMemory (dev) and FileStorage (persistence) with schema migration
- **API Documentation** — Swagger/OpenAPI at `/swagger`

## Architecture

```
backend/src/
├── TaskHub.Core/            # Entities, enums (zero dependencies)
├── Task_hub.Application/    # Services, DTOs, validators, abstractions
├── TaskHub.Infrastructure/  # InMemory and File storage implementations
└── TaskHub.Api/             # Controllers, middleware, extensions

frontend/src/                # React SPA with TypeScript
```

Clean Architecture with dependency inversion: Api → Application → Core ← Infrastructure.

See `docs/c4-architecture.md` for detailed diagrams.

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)

### Backend

```bash
cd backend
dotnet run --project src/TaskHub.Api
```

API available at `http://localhost:5000`. Swagger UI at `http://localhost:5000/swagger`.

### Frontend

```bash
cd frontend
npm install
npm start
```

App available at `http://localhost:3000`.

### Health Check

```bash
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

## Configuration

Edit `backend/src/TaskHub.Api/appsettings.json`:

| Setting | Values | Default | Description |
|---------|--------|---------|-------------|
| StorageProvider | `InMemory`, `File` | `InMemory` | Storage backend |
| FileStorage:Path | directory path | `storage` | File storage location |
| IpRateLimiting | see file | 5 login/min, 10 register/hr | Rate limit rules |

## API Overview

| Endpoint Group | Routes | Auth | Description |
|---------------|--------|------|-------------|
| Auth | `POST /api/v1/auth/{register,login,logout}`, `GET /me` | Public/Required | User authentication |
| Todos | `GET/POST/PUT/DELETE /api/v1/todo/*` | Required + Org | CRUD with concurrency control |
| Organisations | `GET/POST /api/v1/organisations/*` | Required | Org and member management |
| Audit | `GET /api/v1/audit/*` | Admin | Audit log viewing |
| Import/Export | `GET/POST /api/v1/importexport/*` | Required + Org | Data import and export |
| Health | `GET /health/{live,ready}` | Public | Application health |

Full API reference: `docs/api-contract.md`

## Testing

```bash
cd backend
dotnet test TaskHub.sln
```

31 tests across 7 test classes:
- **Auth flow** — Register, login, logout, validation
- **Todo CRUD** — Create, read, update, toggle, delete lifecycle
- **Concurrency** — ETag/If-Match conflict detection
- **Validation** — Input rejection for all endpoints
- **Organisations** — Create, list, add/remove members
- **Import/Export** — JSON/CSV export, inline and file import
- **Authorization** — Unit tests for permission handler

See `docs/testing-strategy.md` for details.

## Documentation

| Document | Description |
|----------|-------------|
| [Project Charter](docs/project-charter.md) | Scope, objectives, constraints |
| [Product Backlog](docs/product-backlog.md) | 96 items across all categories |
| [Requirements](docs/requirements.md) | 3 personas, functional/non-functional requirements, 8 failure paths |
| [Estimation](docs/estimation.md) | Story point breakdown (128 total) |
| [Risk Register](docs/risk-register.md) | 15 risks with mitigations |
| [Data & Privacy](docs/data-privacy.md) | Data classification, access control, audit |
| [Research Log](docs/research-log.md) | 12 technical decisions with alternatives |
| [C4 Architecture](docs/c4-architecture.md) | System context, container, component diagrams |
| [API Contract](docs/api-contract.md) | All endpoints with request/response examples |
| [Data Model](docs/data-model.md) | Entity relationships, schema, migrations |
| [Threat Model](docs/threat-model.md) | STRIDE analysis, attack surface, mitigations |
| [Dependency Register](docs/dependency-register.md) | All packages with versions and licenses |
| [Testing Strategy](docs/testing-strategy.md) | Test architecture, patterns, coverage |
| [Ops Runbook](docs/ops-runbook.md) | Startup, troubleshooting, backup |
| [CHANGELOG](CHANGELOG.md) | Version history |

## Maintenance Scenarios

| Scenario | Document |
|----------|----------|
| Bug fix workflow | [maintenance/bugfix-scenario.md](maintenance/bugfix-scenario.md) |
| Change request workflow | [maintenance/change-request-scenario.md](maintenance/change-request-scenario.md) |
| Security hardening | [maintenance/security-hardening-scenario.md](maintenance/security-hardening-scenario.md) |

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | .NET 9 / ASP.NET Core |
| Frontend | React 19 / TypeScript |
| Auth | Cookie-based (BCrypt, SameSite=Strict) |
| Validation | FluentValidation |
| Logging | Serilog |
| API Docs | Swashbuckle / Swagger |
| Testing | xUnit, FluentAssertions, Moq, Cypress |
| Rate Limiting | AspNetCoreRateLimit |
