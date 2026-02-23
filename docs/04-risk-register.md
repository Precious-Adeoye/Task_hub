# Risk Register — TaskHub

## Risk Matrix

Likelihood: Low (1) / Medium (2) / High (3)
Impact: Low (1) / Medium (2) / High (3)
Score = Likelihood × Impact

## Active Risks

| # | Risk | Category | Likelihood | Impact | Score | Mitigation | Status |
|---|------|----------|-----------|--------|-------|------------|--------|
| R1 | **Data loss with InMemoryStorage** — All data lost on restart | Data | 3 | 3 | 9 | FileStorage alternative with atomic writes; InMemory only for dev/testing | Mitigated |
| R2 | **File corruption during concurrent writes** | Data | 2 | 3 | 6 | SemaphoreSlim serialises all file access; temp file + rename for atomicity | Mitigated |
| R3 | **Cookie theft via XSS** | Security | 1 | 3 | 3 | HttpOnly=true prevents JavaScript access; CSP headers recommended | Mitigated |
| R4 | **CSRF attacks** | Security | 1 | 3 | 3 | SameSite=Strict on all cookies; API-only backend (no form posts from other origins) | Mitigated |
| R5 | **Brute-force login attempts** | Security | 2 | 2 | 4 | IP rate limiting (5 req/min on login); account lockout after 5 failures (15 min) | Mitigated |
| R6 | **User enumeration on registration** | Security | 2 | 2 | 4 | Generic error message returned regardless of whether email/username exists | Mitigated |
| R7 | **Schema migration failure** — FileStorage data unreadable after code change | Data | 1 | 3 | 3 | Versioned schema with tested migration path (v0→v1→v2); migration unit tests | Mitigated |
| R8 | **Optimistic concurrency false conflicts** — Users get 412 errors frequently | UX | 2 | 1 | 2 | Frontend handles 412 with "refresh and retry" message; version field updated per mutation | Accepted |
| R9 | **Large file imports causing memory pressure** | Performance | 2 | 2 | 4 | 10 MB file upload limit; per-row processing without loading all into memory at once | Mitigated |
| R10 | **Missing HTTPS in development** | Security | 2 | 2 | 4 | Secure cookie policy relaxed in tests; UseHttpsRedirection in pipeline; production must use HTTPS | Accepted |
| R11 | **Single-threaded file storage bottleneck** | Performance | 2 | 2 | 4 | SemaphoreSlim(1,1) serialises writes; 5-second cache reduces reads; acceptable for single-instance deployment | Accepted |
| R12 | **No database backup strategy** | Operations | 2 | 3 | 6 | FileStorage data is a single JSON file — easy to copy; no automated backup; export feature provides manual backup | Accepted |
| R13 | **Dependency vulnerabilities** | Security | 2 | 2 | 4 | Pin known-good versions; `dotnet list package --vulnerable` for auditing; update on CVE disclosure | Monitored |
| R14 | **Log file disk exhaustion** | Operations | 1 | 2 | 2 | Serilog rolling daily files; recommend log rotation/cleanup policy in production | Accepted |
| R15 | **Secrets in configuration** — No secret management | Security | 2 | 3 | 6 | No secrets currently stored (no DB password, no API keys); cookie signing uses ASP.NET data protection defaults | Accepted |

## Risk Heat Map

```
Impact ↑
  3 │  R14   R7,R3,R4   R1
    │                    R12,R15
  2 │  R8    R5,R6,R9
    │        R10,R11,R13
  1 │
    └────────────────────→ Likelihood
       1        2        3
```

## Retired Risks

| # | Risk | Resolution |
|---|------|-----------|
| R-A | Tests failing due to missing cookie forwarding | Resolved — CookieContainerHandler + TaskHubWebApplicationFactory |
| R-B | Enum deserialization mismatch (string vs int) | Resolved — JsonStringEnumConverter added globally |
| R-C | Import failing on camelCase JSON | Resolved — PropertyNameCaseInsensitive = true |

## Review Schedule

Risks should be reviewed when:
- A new storage backend is added
- Authentication mechanism changes
- The application is deployed to a shared/production environment
- A security vulnerability is disclosed in a dependency
