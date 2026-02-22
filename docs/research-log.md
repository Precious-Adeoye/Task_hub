# Research Log — TaskHub

Decisions, alternatives evaluated, and rationale for key technical choices.

## 1. Authentication Mechanism

**Decision:** Cookie-based authentication with ASP.NET `CookieAuthenticationDefaults`

**Alternatives Considered:**
- **JWT tokens** — Stateless, but requires token refresh logic, token storage (localStorage vulnerable to XSS), and cannot be invalidated server-side without a blacklist
- **OAuth2 / OpenID Connect** — Overkill for a single-app scenario; adds external dependency
- **Session-based with Redis** — Good for distributed systems but adds infrastructure complexity

**Rationale:** Cookie auth is the simplest secure option for a same-origin SPA. HttpOnly cookies prevent XSS-based theft, SameSite=Strict prevents CSRF, and ASP.NET handles session lifecycle automatically.

**Sources:**
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- [Microsoft Docs — Cookie Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)

## 2. Password Hashing

**Decision:** BCrypt via `BCrypt.Net-Next` NuGet package

**Alternatives Considered:**
- **Argon2** — Winner of the Password Hashing Competition; more configurable but less library maturity in .NET
- **PBKDF2** — Built into ASP.NET Identity; older algorithm with fewer tunables
- **scrypt** — Memory-hard like Argon2 but less widely adopted

**Rationale:** BCrypt is the industry standard with excellent .NET library support. Default work factor adapts to hardware speed. No need for ASP.NET Identity's full framework.

**Sources:**
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [BCrypt.Net-Next GitHub](https://github.com/BcryptNet/bcrypt.net)

## 3. Storage Architecture

**Decision:** Dual storage backends (InMemory + File) behind `IStorage` interface

**Alternatives Considered:**
- **SQLite** — Lightweight relational DB; adds EF Core dependency and migration complexity
- **LiteDB** — NoSQL embedded database; good fit but less familiar to reviewers
- **PostgreSQL** — Production-grade but requires external service

**Rationale:** The `IStorage` interface allows swapping backends without changing business logic. InMemory is fast for tests; FileStorage demonstrates persistence with schema migration. Both avoid external dependencies.

**Sources:**
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Repository Pattern — Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

## 4. Optimistic Concurrency

**Decision:** ETag/If-Match headers with SHA-256 version strings

**Alternatives Considered:**
- **Last-Modified / If-Unmodified-Since** — Lower precision (second-level); doesn't detect sub-second changes
- **Row version / timestamp column** — Database-specific; not applicable to file storage
- **Pessimistic locking** — Blocks other users; poor UX for a web application

**Rationale:** ETag-based concurrency is the HTTP standard approach. Each entity has a `Version` field (GUID) that changes on every mutation. The middleware generates ETags for GET responses, and controllers check If-Match on mutations.

**Sources:**
- [RFC 7232 — Conditional Requests](https://tools.ietf.org/html/rfc7232)
- [Microsoft Docs — Handling Concurrency](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/concurrency)

## 5. Error Response Format

**Decision:** RFC 7807 ProblemDetails for all error responses

**Alternatives Considered:**
- **Custom error envelope** — `{ error: string, code: number }` — Simple but non-standard
- **GraphQL-style errors** — `{ errors: [{ message, path }] }` — REST API, not GraphQL

**Rationale:** ProblemDetails is the IETF standard for HTTP API error responses. ASP.NET has built-in support. Consistent structure helps frontend error handling.

**Sources:**
- [RFC 7807 — Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [Microsoft Docs — ProblemDetails](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors)

## 6. Validation Library

**Decision:** FluentValidation with auto-validation middleware

**Alternatives Considered:**
- **Data Annotations** — Built-in but limited expressiveness; mixes concerns with DTOs
- **Manual validation in controllers** — No reuse; clutters controller code
- **MediatR pipeline validation** — Requires MediatR adoption for all operations

**Rationale:** FluentValidation provides expressive, testable, reusable validation rules separate from DTOs. Auto-validation via `AddFluentValidationAutoValidation()` runs validators before controller actions.

**Sources:**
- [FluentValidation Docs](https://docs.fluentvalidation.net/)
- [FluentValidation.AspNetCore](https://www.nuget.org/packages/FluentValidation.AspNetCore)

## 7. Logging Framework

**Decision:** Serilog with console and rolling file sinks

**Alternatives Considered:**
- **Built-in ILogger** — Less configurable; no structured logging out of the box
- **NLog** — Similar to Serilog; slightly less ecosystem momentum
- **Seq** — Excellent structured log viewer but requires external service

**Rationale:** Serilog provides structured logging with rich context. Console sink for development, rolling file sink for production. Configuration via `appsettings.json`.

**Sources:**
- [Serilog GitHub](https://github.com/serilog/serilog)
- [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)

## 8. Multi-Tenancy Approach

**Decision:** Header-based tenant resolution (`X-Organisation-Id`) with authorisation handler

**Alternatives Considered:**
- **Subdomain-based** — `org1.taskhub.com` — Requires DNS configuration
- **Path-based** — `/api/v1/orgs/{orgId}/todos` — Clutters all routes
- **Database-per-tenant** — Maximum isolation but operational complexity

**Rationale:** Header-based tenancy keeps routes clean and works naturally with SPAs that can set default headers. The `RequireOrganisation` attribute enforces membership checks.

**Sources:**
- [Microsoft Docs — Multi-tenant SaaS patterns](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)

## 9. Frontend Framework

**Decision:** React (Create React App) with TypeScript

**Alternatives Considered:**
- **Angular** — Full framework; heavier for a focused demo
- **Vue.js** — Similar to React; less mainstream in enterprise
- **Svelte** — Newer; less ecosystem maturity

**Rationale:** React is the most widely used frontend framework with the largest ecosystem. TypeScript adds type safety. CRA provides fast setup without build configuration.

**Sources:**
- [React Docs](https://react.dev/)
- [Create React App](https://create-react-app.dev/)

## 10. Rate Limiting

**Decision:** AspNetCoreRateLimit with IP-based rules

**Alternatives Considered:**
- **ASP.NET Core 7+ built-in rate limiting** — Available but less configurable
- **Custom middleware** — Full control but maintenance burden
- **API Gateway rate limiting** — Requires infrastructure (e.g., NGINX, AWS API Gateway)

**Rationale:** AspNetCoreRateLimit is a mature, configurable library. IP-based limiting is sufficient for auth endpoint protection. Rules are defined in `appsettings.json` for easy tuning.

**Sources:**
- [AspNetCoreRateLimit GitHub](https://github.com/stefanprodan/AspNetCoreRateLimit)

## 11. CSV Parsing

**Decision:** Custom CSV parser with quote-aware field splitting

**Alternatives Considered:**
- **CsvHelper** — Popular library; adds dependency for simple use case
- **String.Split** — Doesn't handle quoted fields with commas/newlines

**Rationale:** The import/export CSV format is simple and well-defined (7 columns). A custom parser handles quoted fields and escaped quotes without adding a NuGet dependency. Trade-off: less robust for edge cases than CsvHelper.

## 12. Testing Approach

**Decision:** WebApplicationFactory integration tests with custom cookie handler

**Alternatives Considered:**
- **Unit tests only** — Faster but miss integration issues
- **Docker-based integration tests** — Heavier; not needed without external dependencies
- **Playwright E2E** — Tests full stack but slower and more brittle

**Rationale:** WebApplicationFactory tests the full HTTP pipeline in-process without external dependencies. Custom `CookieContainerHandler` handles cookie-based auth in tests. Complements unit tests for authorization logic.

**Sources:**
- [Microsoft Docs — Integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
