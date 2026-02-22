# Product Backlog — TaskHub

Items are ordered by priority within each category. Status: Done, In Progress, or Planned.

## Authentication & Security

| # | Item | Priority | Status |
|---|------|----------|--------|
| 1 | User registration with email, username, password | High | Done |
| 2 | BCrypt password hashing | High | Done |
| 3 | Cookie-based session authentication (HttpOnly, Secure, SameSite) | High | Done |
| 4 | Login with username or email | High | Done |
| 5 | Account lockout after 5 failed attempts (15 min) | High | Done |
| 6 | Logout with cookie clearing | High | Done |
| 7 | GET /me endpoint for current user | Medium | Done |
| 8 | IP rate limiting on login (5/min) and register (10/hr) | High | Done |
| 9 | CSRF protection via SameSite=Strict cookies | High | Done |
| 10 | Input validation on all auth endpoints (FluentValidation) | High | Done |
| 11 | Prevent user enumeration on registration | High | Done |
| 12 | JWT or OAuth2 support | Low | Planned |
| 13 | Password reset flow | Low | Planned |
| 14 | Two-factor authentication | Low | Planned |

## Organisation & Multi-Tenancy

| # | Item | Priority | Status |
|---|------|----------|--------|
| 15 | Create organisation (creator becomes OrgAdmin) | High | Done |
| 16 | List user's organisations | High | Done |
| 17 | Get organisation details | Medium | Done |
| 18 | Add member by email | High | Done |
| 19 | Remove member | High | Done |
| 20 | Change member role (Member ↔ OrgAdmin) | High | Done |
| 21 | Organisation-scoped data isolation | High | Done |
| 22 | X-Organisation-Id header for tenant context | High | Done |
| 23 | Custom RequireOrganisation authorization attribute | High | Done |
| 24 | Organisation deletion | Low | Planned |
| 25 | Organisation settings/preferences | Low | Planned |

## Todo Management

| # | Item | Priority | Status |
|---|------|----------|--------|
| 26 | Create todo with title, description, priority, tags, due date | High | Done |
| 27 | List todos with pagination (page, pageSize) | High | Done |
| 28 | Filter by status, overdue, tag | High | Done |
| 29 | Sort by createdAt, dueDate, priority (asc/desc) | High | Done |
| 30 | Update todo fields | High | Done |
| 31 | Toggle todo status (Open ↔ Done) | High | Done |
| 32 | Soft delete (marks DeletedAt) | High | Done |
| 33 | Restore soft-deleted todo | High | Done |
| 34 | Hard delete (admin only, permanent) | High | Done |
| 35 | Include/exclude deleted items in listing | Medium | Done |
| 36 | Optimistic concurrency via ETag/If-Match (412 on conflict) | High | Done |
| 37 | X-Total-Count response header for pagination | Medium | Done |
| 38 | Todo assignment to specific users | Low | Planned |
| 39 | Subtasks / checklist items | Low | Planned |
| 40 | Recurring todos | Low | Planned |

## Import / Export

| # | Item | Priority | Status |
|---|------|----------|--------|
| 41 | Export todos as JSON | High | Done |
| 42 | Export todos as CSV | High | Done |
| 43 | Import todos from JSON body | High | Done |
| 44 | Import todos from file upload (multipart, 10 MB limit) | High | Done |
| 45 | ClientProvidedId-based idempotent import | High | Done |
| 46 | Optional overwrite mode | Medium | Done |
| 47 | Per-row validation with error reporting | High | Done |
| 48 | Import/export template download | Medium | Done |
| 49 | Case-insensitive JSON deserialization | Medium | Done |

## Audit & Observability

| # | Item | Priority | Status |
|---|------|----------|--------|
| 50 | Audit log for all mutations (create, update, delete, restore) | High | Done |
| 51 | Audit log for auth events (login failure, logout) | High | Done |
| 52 | Correlation ID middleware (X-Correlation-ID header) | High | Done |
| 53 | Audit log listing with date range + pagination (admin only) | High | Done |
| 54 | Audit summary by action type | Medium | Done |
| 55 | Structured logging with Serilog (console + file) | High | Done |
| 56 | Health check endpoints (/health/live, /health/ready) | Medium | Done |
| 57 | Centralised metrics dashboard | Low | Planned |

## API Quality

| # | Item | Priority | Status |
|---|------|----------|--------|
| 58 | /api/v1 route prefix on all controllers | High | Done |
| 59 | RFC 7807 ProblemDetails for all error responses | High | Done |
| 60 | ProducesResponseType attributes on all 25 actions | Medium | Done |
| 61 | Produces("application/json") on all controllers | Medium | Done |
| 62 | Swagger/OpenAPI with cookie auth + org header | High | Done |
| 63 | Swagger example providers (12 providers) | Medium | Done |
| 64 | JsonStringEnumConverter for enum serialization | Medium | Done |
| 65 | ETag middleware for GET response caching | Medium | Done |

## Infrastructure & Storage

| # | Item | Priority | Status |
|---|------|----------|--------|
| 66 | InMemoryStorage (concurrent dictionaries) | High | Done |
| 67 | FileStorage (JSON file with atomic writes) | High | Done |
| 68 | Configurable storage provider via appsettings | High | Done |
| 69 | Schema migration v0 → v1 → v2 | High | Done |
| 70 | 5-second file cache to reduce disk I/O | Medium | Done |
| 71 | SemaphoreSlim for thread-safe file access | High | Done |
| 72 | Database persistence (EF Core + PostgreSQL) | Low | Planned |

## Frontend

| # | Item | Priority | Status |
|---|------|----------|--------|
| 73 | Login / Register forms | High | Done |
| 74 | Organisation creation and switching | High | Done |
| 75 | Todo list with filters and pagination | High | Done |
| 76 | Create todo form | High | Done |
| 77 | Edit todo inline with concurrency handling | High | Done |
| 78 | Soft delete, restore, hard delete buttons | High | Done |
| 79 | Admin panel with audit log viewer | High | Done |
| 80 | Import/export UI | High | Done |
| 81 | Add member form in admin panel | High | Done |
| 82 | Deleted-item visual distinction (.deleted CSS) | Medium | Done |

## Testing

| # | Item | Priority | Status |
|---|------|----------|--------|
| 83 | Auth flow integration tests | High | Done |
| 84 | Todo CRUD integration tests (8 tests) | High | Done |
| 85 | Validation integration tests (5 tests) | High | Done |
| 86 | Organisation management tests (5 tests) | High | Done |
| 87 | Import/export integration tests (5 tests) | High | Done |
| 88 | Concurrency (ETag) integration tests | High | Done |
| 89 | Authorization unit tests (permissions) | High | Done |
| 90 | FileStorage migration unit tests | High | Done |
| 91 | Cypress E2E test scaffolding | Medium | Done |
| 92 | Full Cypress E2E suite | Low | Planned |

## Documentation

| # | Item | Priority | Status |
|---|------|----------|--------|
| 93 | README.md | High | Done |
| 94 | CHANGELOG.md | High | Done |
| 95 | docs/ (14 documents) | High | Done |
| 96 | maintenance/ (3 scenario documents) | High | Done |
