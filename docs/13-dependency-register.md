# Dependency Register — TaskHub

## Runtime Dependencies

### TaskHub.Api

| Package | Version | Purpose | License | Risk Notes |
|---------|---------|---------|---------|------------|
| AspNetCoreRateLimit | 5.0.0 | IP-based rate limiting on auth endpoints | MIT | Mature, widely used |
| BCrypt.Net-Next | 4.1.0 | Password hashing with BCrypt algorithm | MIT | Fork of BCrypt.Net with .NET 6+ support |
| FluentValidation.AspNetCore | 11.3.1 | Automatic request validation in MVC pipeline | Apache 2.0 | Auto-validation integrates with model binding |
| Microsoft.AspNetCore.Authentication.Cookies | 2.3.9 | Cookie-based session authentication | MIT | Microsoft official package |
| Microsoft.AspNetCore.OpenApi | 9.0.13 | OpenAPI metadata for Swagger | MIT | .NET 9 official |
| Serilog.AspNetCore | 10.0.0 | Structured logging integration | Apache 2.0 | Standard .NET logging library |
| Serilog.Sinks.Console | 6.1.1 | Console log output | Apache 2.0 | Development/debugging |
| Serilog.Sinks.File | 7.0.0 | Rolling file log output | Apache 2.0 | Production log persistence |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger UI and OpenAPI doc generation | MIT | Industry standard for .NET APIs |
| Swashbuckle.AspNetCore.Filters | 8.0.2 | Request/response examples in Swagger | MIT | Extends Swashbuckle with example providers |

### TaskHub.Core

| Package | Version | Purpose | License | Risk Notes |
|---------|---------|---------|---------|------------|
| MediatR | 14.0.0 | Mediator pattern (referenced, minimal use) | Apache 2.0 | Could be removed if not used for CQRS |

### Task_hub.Application

| Package | Version | Purpose | License | Risk Notes |
|---------|---------|---------|---------|------------|
| BCrypt.Net-Next | 4.1.0 | Password hashing in AuthService | MIT | Same as API layer |
| FluentValidation | 11.11.0 | Validation rule definitions | Apache 2.0 | Core library (no ASP.NET coupling) |

### TaskHub.Infrastructure

No external NuGet dependencies — only project references to Core and Application.

## Test Dependencies

### TaskHub.Tests

| Package | Version | Purpose | License |
|---------|---------|---------|---------|
| coverlet.collector | 6.0.2 | Code coverage collection | MIT |
| FluentAssertions | 7.0.0 | Expressive test assertions | Apache 2.0 |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.1 | WebApplicationFactory for integration tests | MIT |
| Microsoft.NET.Test.Sdk | 17.12.0 | Test platform SDK | MIT |
| Moq | 4.20.72 | Mock framework for unit tests | BSD-3-Clause |
| xunit | 2.9.2 | Test framework | Apache 2.0 |
| xunit.runner.visualstudio | 2.8.2 | VS/CLI test runner | Apache 2.0 |

## Frontend Dependencies

### React Application

| Package | Version | Purpose | License |
|---------|---------|---------|---------|
| react | 19.x | UI framework | MIT |
| react-dom | 19.x | DOM rendering | MIT |
| react-scripts | 5.x | CRA build tooling | MIT |
| typescript | 4.x | Type safety | Apache 2.0 |
| cypress | 13.x | E2E testing | MIT |

## Framework Dependencies

| Component | Version | Notes |
|-----------|---------|-------|
| .NET | 9.0 | LTS candidate; latest stable |
| ASP.NET Core | 9.0 | Web framework |
| Node.js | 18+ | Frontend build (CRA) |

## Dependency Graph

```
TaskHub.Api
  ├── Task_hub.Application
  │     └── TaskHub.Core
  └── TaskHub.Infrastructure
        ├── TaskHub.Core
        └── Task_hub.Application

TaskHub.Tests
  ├── TaskHub.Api (for WebApplicationFactory)
  ├── Task_hub.Application
  ├── TaskHub.Core
  └── TaskHub.Infrastructure
```

## Update Policy

| Category | Policy |
|----------|--------|
| Security patches | Apply immediately upon CVE disclosure |
| Minor versions | Review monthly; update if no breaking changes |
| Major versions | Evaluate migration effort; schedule in next sprint |
| .NET runtime | Follow Microsoft LTS schedule |

## Vulnerability Audit

Run periodically:
```bash
dotnet list package --vulnerable
npm audit          # in frontend/
```

## Licensing Summary

All dependencies use permissive open-source licenses (MIT, Apache 2.0, BSD-3-Clause). No copyleft (GPL) dependencies. Safe for commercial use.
