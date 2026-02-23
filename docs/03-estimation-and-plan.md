# Estimation — TaskHub

## Estimation Approach

Story points use a Fibonacci scale (1, 2, 3, 5, 8, 13) where 1 point ≈ a trivial change and 13 ≈ a multi-day effort with significant unknowns.

## Completed Work

### Phase 0.0.1 — Initial Implementation

| Item | Points | Actual Effort | Notes |
|------|--------|---------------|-------|
| Core entities + enums | 3 | As estimated | 6 entities, 3 enums |
| Cookie-based auth with BCrypt + lockout | 8 | Slightly over | Lockout logic and session management complex |
| RBAC (RequireOrganisation attribute + handler) | 5 | As estimated | Custom IAuthorizationHandler |
| Multi-tenant todo CRUD | 8 | As estimated | Organisation-scoped isolation, 8 endpoints |
| Optimistic concurrency (ETag/If-Match) | 5 | As estimated | Middleware + per-entity version tracking |
| Soft delete / restore / hard delete | 3 | As estimated | DeletedAt marker, admin-only hard delete |
| Audit logging + correlation IDs | 5 | As estimated | Middleware + service + per-request ID |
| Import/export (JSON + CSV) | 5 | Slightly over | CSV parsing edge cases, idempotency |
| IP rate limiting | 2 | As estimated | AspNetCoreRateLimit config |
| FluentValidation (6 validators) | 3 | As estimated | Auth, todo, org request validators |
| Serilog (console + file) | 2 | As estimated | Rolling file, enrichment |
| ETag middleware | 3 | As estimated | SHA-256 body hash, 304 support |
| ProblemDetails middleware | 3 | As estimated | Exception → RFC 7807 mapping |
| Swagger/OpenAPI + examples | 3 | As estimated | Cookie auth, org header, 12 example providers |
| InMemoryStorage | 3 | As estimated | ConcurrentDictionary-based |
| FileStorage + migrations | 8 | Over estimate | Atomic writes, SemaphoreSlim, 3 schema versions |
| React frontend | 8 | As estimated | Auth, org, todos, admin, filters |
| **Subtotal** | **77** | | |

### Phase 0.1.0–0.5.0 — Refactoring & Fixes

| Item | Points | Notes |
|------|--------|-------|
| Test consolidation (merge test projects) | 2 | Move 1 file, remove project |
| API project cleanup (move validators, examples) | 2 | 3 file moves |
| Controller refactoring (extract services) | 5 | 11 methods → 4 services/extensions |
| Application layer reorganisation | 3 | 10 → 6 folders |
| Backend fixes (routing, health, security) | 5 | Route prefix, health, enumeration fix |
| **Subtotal** | **17** | |

### Unreleased — Frontend + Tests + Docs

| Item | Points | Notes |
|------|--------|-------|
| Frontend fixes (edit, delete, restore, add member) | 8 | 10 changes across 4 files |
| Fix 2 failing tests (cookie handler) | 3 | Custom WebApplicationFactory + DelegatingHandler |
| 23 new integration tests | 8 | 5 test classes, comprehensive coverage |
| ProblemDetails consistency + ProducesResponseType | 3 | All 5 controllers updated |
| JsonStringEnumConverter + case-insensitive import | 2 | 2 bug fixes |
| CHANGELOG.md | 2 | Full history |
| Documentation (14 docs + README + maintenance) | 8 | Comprehensive documentation suite |
| **Subtotal** | **34** | |

## Summary

| Category | Story Points |
|----------|-------------|
| Initial Implementation | 77 |
| Refactoring & Fixes | 17 |
| Frontend + Tests + Docs | 34 |
| **Total** | **128** |

## Velocity Observations

- Backend CRUD and auth patterns are well-understood — estimates were accurate
- FileStorage complexity was underestimated due to migration logic and atomicity concerns
- Frontend work scales with the number of distinct UI states (edit, delete, confirm)
- Test infrastructure (cookie handler, custom factory) was an unplanned but necessary investment
- Documentation effort is proportional to feature count, not complexity

## Future Estimates

| Item | Points | Rationale |
|------|--------|-----------|
| EF Core + PostgreSQL migration | 13 | New project, migration scripts, connection pooling |
| JWT authentication option | 8 | Token generation, refresh, middleware changes |
| Real-time updates (SignalR) | 8 | Hub, client integration, connection management |
| Full Cypress E2E suite | 5 | 15–20 tests, mostly UI automation |
| CI/CD pipeline | 5 | GitHub Actions, build + test + deploy |
