# Project Charter — TaskHub

## 1. Purpose

TaskHub is a secure, multi-tenant todo management platform that demonstrates production-ready software engineering practices. It serves as a portfolio piece and technical assessment submission for elios.tech.

## 2. Objectives

| # | Objective | Success Metric |
|---|-----------|----------------|
| O1 | Deliver a working full-stack application | Backend API + React frontend running locally |
| O2 | Demonstrate Clean Architecture in .NET 9 | Four-layer separation: Core → Application → Infrastructure → Api |
| O3 | Implement enterprise security patterns | Cookie auth, RBAC, rate limiting, audit logging |
| O4 | Support multi-tenancy | Organisation-scoped data isolation verified by tests |
| O5 | Provide comprehensive documentation | 14+ docs covering architecture, operations, and process |

## 3. Scope

### In Scope

- RESTful API with 25 endpoints across 5 controllers
- Cookie-based authentication with BCrypt hashing and account lockout
- Role-based access control (Member / OrgAdmin)
- Multi-tenant todo CRUD with soft delete, restore, and hard delete
- Optimistic concurrency via ETag / If-Match (HTTP 412)
- Audit logging with correlation IDs
- JSON and CSV import/export with idempotency
- Input validation (FluentValidation)
- IP rate limiting on auth endpoints
- Switchable storage backends (InMemory / File)
- File storage with atomic writes and schema migration
- React frontend with auth, org management, todos, admin panel
- Swagger/OpenAPI documentation
- Integration and unit tests (31 tests)
- Cypress E2E test scaffolding

### Out of Scope

- Database-backed persistence (EF Core / SQL)
- Real-time features (WebSockets / SignalR)
- Email notifications
- CI/CD pipeline automation
- Container orchestration (Kubernetes)
- Production deployment

## 4. Stakeholders

| Role | Name / Group | Responsibility |
|------|-------------|----------------|
| Developer | Tola | Design, implementation, testing, documentation |
| Reviewer | elios.tech | Evaluate technical assessment |

## 5. Timeline

| Phase | Deliverable | Status |
|-------|-------------|--------|
| 0.0.1 | Initial implementation (all core features) | Done |
| 0.1.0 | Test consolidation | Done |
| 0.2.0 | API project cleanup | Done |
| 0.3.0 | Controller refactoring | Done |
| 0.4.0 | Application layer reorganisation | Done |
| 0.5.0 | Backend fixes (routing, health, security) | Done |
| Unreleased | Frontend fixes, more tests, documentation | Done |

## 6. Constraints

- No external database — storage is file-based or in-memory
- Single-developer project
- .NET 9 / React / no paid services

## 7. Risks (Summary)

See `docs/risk-register.md` for the full register. Key risks:

- **Data loss** — InMemory storage is volatile; FileStorage uses atomic writes to mitigate
- **Concurrency** — File-based locking via SemaphoreSlim; no distributed lock support
- **Security** — Cookie auth without HTTPS in dev; rate limiting only on auth endpoints

## 8. Definition of Done

- [ ] All 31 integration/unit tests pass
- [ ] Frontend builds without errors
- [ ] Swagger documentation accessible at `/swagger`
- [ ] Health endpoints respond at `/health/live` and `/health/ready`
- [ ] All 14 documentation files present in `docs/`
- [ ] CHANGELOG.md up to date
- [ ] README.md comprehensive
