# Changelog

All notable changes to the TaskHub project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- Edit todo inline form with optimistic concurrency (If-Match header, 412 handling)
- Hard delete button for admin users on soft-deleted items with confirmation dialog
- Restore button for soft-deleted items
- Add Member admin panel with email/role form
- `name` attributes on auth form inputs for testability
- `deletedAt` field on frontend Todo type for deleted-item styling
- `JsonStringEnumConverter` for proper enum string serialization/deserialization
- Case-insensitive JSON deserialization in import service
- `[ProducesResponseType]` attributes on all 25 controller actions
- `[Produces("application/json")]` on all controllers
- 23 new integration tests (31 total): todo CRUD, validation, organisation management, import/export
- `TaskHubWebApplicationFactory` and `CookieContainerHandler` test helpers for cookie-based auth testing

### Fixed
- Auth integration tests failing due to missing cookie forwarding between HTTP requests
- Concurrency test failing due to secure cookie policy over HTTP test server
- CorrelationId in audit logs now uses custom `X-Correlation-ID` header instead of `TraceIdentifier`
- Import via file upload failing on camelCase JSON input

### Changed
- All error responses now use RFC 7807 ProblemDetails format (412, 400, 401)
- Cypress E2E selectors updated to match actual DOM structure (`.closest('.todo-item')`, `button[title]`)
- Soft-deleted todo items now have `.deleted` CSS class with visual distinction

## [0.5.0] - Backend Fixes

### Added
- `/api/v1` route prefix on all 5 controllers
- `/health/live` and `/health/ready` endpoints
- Audit logging for login failures and logout events
- If-Match concurrency checks on soft-delete, restore, and hard-delete endpoints
- Swagger example providers for all endpoint request/response types (12 providers)
- CSRF protection strategy documented in `AuthExtensions.cs`

### Fixed
- User enumeration vulnerability in registration (now returns generic error message)
- Rate limiting path mismatch (rules now match `/api/v1/auth/*` routes)

## [0.4.0] - Application Layer Reorganisation

### Changed
- Reorganised Application layer from 10 scattered folders to 6 purpose-driven folders
- `Abstraction/` renamed to `Abstractions/` with all interfaces consolidated
- `Auth/` split into `Abstractions/IAuthService` + `Services/AuthService`
- `Data/` moved to `Services/OrganisationContext`
- `Migration/` split into `Abstractions/IMigrationService` + `Services/MigrationService`
- `Mapping/` merged into `Extensions/MappingExtensions`
- `Dto/Response/Audit/` flattened to `Dto/AuditDto.cs`

## [0.3.0] - Controller Refactoring

### Changed
- Extracted 11 private methods from 5 controllers into proper services and extensions
- Created `ClaimsPrincipalExtensions.GetUserId()` (replaces duplicated `GetCurrentUserId`)
- Created `IAuditService` / `AuditService` (replaces duplicated `Audit` methods)
- Created `MappingExtensions.ToResponse()` (replaces duplicated `MapToResponse` methods)
- Moved `SignInUserAsync` into `IAuthService.SignInAsync`

## [0.2.0] - API Project Cleanup

### Changed
- Moved `Validators/` (3 files) from API to Application layer
- Moved `Examples/TodoExamples.cs` into `Extensions/`
- Reduced API project from 5 folders to 3 (Controller, Extensions, Middleware)

## [0.1.0] - Test Consolidation

### Changed
- Merged `TaskHub.Infrastructure.Tests` into `TaskHub.Tests`
- Moved `FileStorageMigrationTests.cs`, updated namespace

### Removed
- `TaskHub.Infrastructure.Tests` project (redundant separate project)

## [0.0.1] - Initial Implementation

### Added
- .NET 9 Clean Architecture: Core, Application, Infrastructure, Api layers
- Cookie-based authentication with BCrypt password hashing and account lockout
- RBAC authorization (Member vs OrgAdmin) with custom `RequireOrganisation` attribute
- Multi-tenant todo CRUD with organisation-scoped data isolation
- Optimistic concurrency via ETag/If-Match with HTTP 412 responses
- Soft delete, restore, and admin-only hard delete
- Audit logging for all mutations with correlation IDs
- JSON and CSV import/export with clientProvidedId idempotency
- IP rate limiting on auth endpoints (AspNetCoreRateLimit)
- Input validation with FluentValidation (auth, todo, organisation requests)
- Structured logging with Serilog (console + rolling file)
- Correlation ID middleware (X-Correlation-ID header)
- ETag middleware for GET response caching (SHA-256 body hash)
- ProblemDetails middleware for RFC 7807 error responses
- Swagger/OpenAPI documentation with cookie auth and org header definitions
- Switchable storage backends: InMemoryStorage and FileStorage
- FileStorage with atomic writes and schema migration (v0 → v1 → v2)
- React frontend with auth flow, org management, todo list, filters, pagination
- Admin panel with audit log viewer and import/export UI
- Cypress E2E test scaffolding
