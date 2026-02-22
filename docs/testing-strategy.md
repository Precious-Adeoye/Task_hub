# Testing Strategy — TaskHub

## 1. Overview

TaskHub uses a layered testing approach: unit tests for isolated logic, integration tests for API behaviour, and E2E scaffolding for full-stack verification.

| Layer | Framework | Count | Focus |
|-------|-----------|-------|-------|
| Unit | xUnit + Moq | 2 | Authorization handler logic |
| Integration | xUnit + WebApplicationFactory | 29 | Full HTTP pipeline (controllers, middleware, storage) |
| E2E | Cypress | Scaffolded | Browser-based UI flows |

**Total: 31 tests** (all passing)

## 2. Test Architecture

```
tests/TaskHub.Tests/
├── Authorization/
│   └── PermissionTests.cs          # Unit tests (2)
├── Concurrency/
│   └── ConcurrencyTests.cs         # Integration (2)
├── Helpers/
│   ├── TaskHubWebApplicationFactory.cs  # Custom WebApplicationFactory
│   └── CookieContainerHandler.cs        # Cookie management for tests
├── Infrastructure/
│   └── FileStorageMigrationTests.cs     # Unit tests (2 — schema migration)
└── Integration/
    ├── AuthFlowTests.cs             # Auth flow (2)
    ├── TodoCrudTests.cs             # Todo CRUD (8)
    ├── ValidationTests.cs           # Input validation (5)
    ├── OrganisationTests.cs         # Org management (5)
    └── ImportExportTests.cs         # Import/export (5)
```

## 3. Test Infrastructure

### TaskHubWebApplicationFactory

Custom `WebApplicationFactory<Program>` that:
- Relaxes `CookieSecurePolicy` to `SameAsRequest` (test server uses HTTP)
- Provides `CreateClientWithCookies()` for cookie-based auth testing
- Uses InMemoryStorage by default (fast, isolated)

### CookieContainerHandler

`DelegatingHandler` that automatically:
- Stores `Set-Cookie` headers from responses
- Forwards stored cookies on subsequent requests
- Enables stateful auth flows in integration tests

## 4. Test Categories

### 4.1 Unit Tests

**PermissionTests** (2 tests)
- `OrgMember_ShouldHaveAccess` — Verifies the authorization handler grants access when user is a member
- `NonMember_ShouldBeDenied` — Verifies access denied for non-members

**FileStorageMigrationTests** (2 tests)
- Tests schema migration from v0 → v1 (adds Version field)
- Tests schema migration from v1 → v2 (adds Description field)

### 4.2 Integration Tests

**AuthFlowTests** (2 tests)
- `Register_Login_Me_Logout_ShouldWork` — Full auth lifecycle with cookie forwarding
- `Register_WithWeakPassword_ShouldReturn400` — Validation rejection

**ConcurrencyTests** (2 tests)
- `ConcurrentUpdates_ShouldReturn412` — Two simultaneous updates, one gets 412
- `Update_WithCorrectETag_ShouldSucceed` — Happy path with valid If-Match

**TodoCrudTests** (8 tests)
- Create todo (201, correct fields returned)
- Get todo with ETag header
- List todos with pagination (X-Total-Count header)
- Update todo fields
- Toggle status (Open → Done)
- Full lifecycle: soft-delete → restore → hard-delete
- Get nonexistent todo (404)
- Filter by status

**ValidationTests** (5 tests)
- Short password rejected (400)
- Invalid email rejected (400)
- Empty todo title rejected (400)
- Too-short todo title rejected (400)
- Too-short org name rejected (400)

**OrganisationTests** (5 tests)
- Create organisation (201)
- List user's organisations
- Add member by email
- Duplicate member rejected (400)
- Nonexistent user rejected (400)

**ImportExportTests** (5 tests)
- JSON export (200, correct content type)
- CSV export (200, correct content type)
- JSON inline import (accepted count = 2)
- JSON file upload import (accepted count ≥ 1)
- Template download (200, correct content type)

## 5. Test Patterns

### Authentication Pattern

All integration tests follow this pattern:
```csharp
var client = _factory.CreateClientWithCookies();
var uniqueId = Guid.NewGuid().ToString()[..8];

// Register (auto-signs-in via cookie)
await client.PostAsJsonAsync("/api/v1/auth/register", new {
    username = $"user_{uniqueId}",
    email = $"user_{uniqueId}@test.com",
    password = "Test123!@#"
});

// Create org and set header
var orgResponse = await client.PostAsJsonAsync("/api/v1/organisations", new {
    name = $"Org_{uniqueId}"
});
var org = await orgResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
client.DefaultRequestHeaders.Add("X-Organisation-Id", org!.Id.ToString());

// Now test authenticated, org-scoped operations...
```

### Isolation Pattern

- Each test creates unique users/orgs using `Guid.NewGuid()` prefix
- No shared state between tests
- InMemoryStorage resets per factory instance
- Tests can run in parallel (xUnit default)

### Assertion Pattern

Uses FluentAssertions for readable assertions:
```csharp
response.StatusCode.Should().Be(HttpStatusCode.Created);
todo!.Title.Should().Be("Test Todo");
response.Headers.Contains("ETag").Should().BeTrue();
```

## 6. What Is Not Tested

| Gap | Risk | Reason |
|-----|------|--------|
| FileStorage concurrent access | Medium | Integration tests use InMemoryStorage |
| Rate limiting behaviour | Low | Relies on AspNetCoreRateLimit library tests |
| CORS enforcement | Low | Browser-enforced; not testable via HttpClient |
| Swagger generation | Low | Visual verification sufficient |
| Frontend React components | Medium | Cypress scaffolded but not complete |
| Admin-only hard delete auth check | Low | Tested implicitly via full lifecycle test |

## 7. Running Tests

```bash
# Run all tests
dotnet test TaskHub.sln

# Run with verbose output
dotnet test TaskHub.sln --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~TodoCrudTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 8. Cypress E2E (Scaffolded)

```bash
cd frontend
npx cypress open    # Interactive mode
npx cypress run     # Headless mode
```

**Existing test files:**
- `cypress/e2e/member-flow.cy.ts` — Todo CRUD with filters
- `cypress/e2e/admin-flow.cy.ts` — Delete, restore, hard delete flows

## 9. Quality Gates

Before merging:
1. All 31 tests pass (`dotnet test`)
2. Zero build warnings in Application/Core/Infrastructure layers
3. Frontend builds without errors (`npm run build`)
4. No new `dotnet list package --vulnerable` findings
