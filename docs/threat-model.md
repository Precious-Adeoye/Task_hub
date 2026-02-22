# Threat Model — TaskHub

## 1. System Overview

TaskHub is a multi-tenant todo management API with a React SPA frontend. Authentication is cookie-based. Data is stored in-memory or in a local JSON file.

## 2. Trust Boundaries

```
┌─────────────────────────────────────────────────────┐
│ Trust Boundary: Internet                            │
│                                                     │
│  ┌───────────┐                                     │
│  │  Browser   │ ─── HTTPS ───┐                     │
│  │  (React)   │              │                     │
│  └───────────┘              │                     │
│                              ▼                     │
│  ┌──────────────────────────────────────────┐      │
│  │ Trust Boundary: API Server               │      │
│  │                                          │      │
│  │  ┌────────────────────────┐              │      │
│  │  │  Middleware Pipeline    │              │      │
│  │  │  (Rate Limit, Auth,    │              │      │
│  │  │   CORS, Validation)    │              │      │
│  │  └──────────┬─────────────┘              │      │
│  │             ▼                            │      │
│  │  ┌──────────────────────┐               │      │
│  │  │  Application Logic    │               │      │
│  │  │  (Services, Handlers) │               │      │
│  │  └──────────┬───────────┘               │      │
│  │             ▼                            │      │
│  │  ┌────────────────────────────────┐     │      │
│  │  │ Trust Boundary: Storage         │     │      │
│  │  │  InMemory / File (local disk)   │     │      │
│  │  └────────────────────────────────┘     │      │
│  └──────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────┘
```

## 3. STRIDE Analysis

### S — Spoofing

| Threat | Risk | Mitigation | Residual Risk |
|--------|------|------------|---------------|
| Stolen session cookie | Medium | HttpOnly (no JS access), Secure (HTTPS only), SameSite=Strict | Cookie theft via network MITM if HTTPS misconfigured |
| Credential brute-force | Medium | Account lockout (5 attempts/15 min), IP rate limiting (5 login/min) | Distributed attacks from many IPs |
| User enumeration on register | Low | Generic error message regardless of whether email/username exists | Timing side-channels possible |

### T — Tampering

| Threat | Risk | Mitigation | Residual Risk |
|--------|------|------------|---------------|
| Request body manipulation | Medium | FluentValidation on all inputs, strong typing | Custom validators might miss edge cases |
| Concurrent data modification | Medium | ETag/If-Match concurrency control, 412 on mismatch | No pessimistic locking available |
| File storage corruption | Low | Atomic writes (temp file + rename), SemaphoreSlim serialisation | OS crash during rename (unlikely) |
| CSV injection in export | Low | Values are quoted in CSV export | Could add explicit formula prefix stripping |

### R — Repudiation

| Threat | Risk | Mitigation | Residual Risk |
|--------|------|------------|---------------|
| User denies performing action | Low | Audit log records ActorUserId, Timestamp, CorrelationId for every mutation | Audit logs stored in same file as data (no tamper-proof chain) |
| Missing audit trail | Low | Login failures and logouts also logged | Successful logins not explicitly logged (session creation implicit) |

### I — Information Disclosure

| Threat | Risk | Mitigation | Residual Risk |
|--------|------|------------|---------------|
| Cross-tenant data access | High | Organisation-scoped queries, RequireOrganisation authorisation attribute | Logic bug in a new endpoint could bypass scoping |
| Password exposure | Low | BCrypt hashing, password never returned in responses | Password hash stored in plain JSON file |
| Verbose error messages | Low | ProblemDetails with controlled messages, no stack traces in production | Development mode may leak details |
| Log file exposure | Medium | Serilog writes to local files | No log file access control beyond OS permissions |

### D — Denial of Service

| Threat | Risk | Mitigation | Residual Risk |
|--------|------|------------|---------------|
| Auth endpoint flooding | Medium | IP rate limiting (5 login/min, 10 register/hr) | Rate limiting only on auth endpoints |
| Large file upload | Medium | 10 MB file upload limit on import | No request body size limit on JSON import endpoint |
| SemaphoreSlim contention | Low | Single-writer serialisation for FileStorage | Heavy write load will queue requests |

### E — Elevation of Privilege

| Threat | Risk | Mitigation | Residual Risk |
|--------|------|------------|---------------|
| Member acts as admin | Low | RequireOrganisation(RequireAdmin=true) on admin endpoints | Must verify all new endpoints apply correct attribute |
| Self-role elevation | Low | Explicit check prevents users from changing own role | No check on organisation creator changing their own role |
| Cross-org admin access | Low | Membership verified per-organisation per-request | None identified |

## 4. Attack Surface

### External Entry Points

| Entry Point | Protocol | Authentication | Rate Limited |
|-------------|----------|---------------|-------------|
| POST /api/v1/auth/register | HTTPS | None | Yes (10/hr) |
| POST /api/v1/auth/login | HTTPS | None | Yes (5/min) |
| POST /api/v1/auth/logout | HTTPS | Cookie | No |
| GET /api/v1/auth/me | HTTPS | Cookie | No |
| GET/POST/PUT/DELETE /api/v1/todo/* | HTTPS | Cookie + Org | No |
| GET/POST/PUT/DELETE /api/v1/organisations/* | HTTPS | Cookie | No |
| GET /api/v1/audit/* | HTTPS | Cookie + Admin | No |
| GET/POST /api/v1/importexport/* | HTTPS | Cookie + Org | No |
| GET /api/v1/importexport/template | HTTPS | None | No |
| GET /health/live | HTTPS | None | No |
| GET /health/ready | HTTPS | None | No |
| GET /swagger/* | HTTPS | None | No |

### Data Stores

| Store | Access | Protection |
|-------|--------|------------|
| InMemory (ConcurrentDictionary) | In-process only | Process isolation |
| File (taskhub-data.json) | Local filesystem | OS file permissions |
| Log files (logs/taskhub-*.txt) | Local filesystem | OS file permissions |

## 5. Mitigations Summary

| Category | Control | Status |
|----------|---------|--------|
| Authentication | BCrypt password hashing | Implemented |
| Authentication | Account lockout (5 attempts / 15 min) | Implemented |
| Authentication | IP rate limiting on auth endpoints | Implemented |
| Session | HttpOnly, Secure, SameSite=Strict cookies | Implemented |
| Session | 7-day sliding expiration | Implemented |
| Authorization | Organisation membership checks | Implemented |
| Authorization | Role-based access (Member / OrgAdmin) | Implemented |
| Input Validation | FluentValidation on all request DTOs | Implemented |
| Error Handling | ProblemDetails (no stack traces) | Implemented |
| Concurrency | ETag / If-Match with 412 responses | Implemented |
| Audit | Immutable audit log with correlation IDs | Implemented |
| Data Isolation | Organisation-scoped queries | Implemented |
| Anti-Enumeration | Generic registration error messages | Implemented |
| Transport | HTTPS redirection | Implemented |
| CORS | Restricted to localhost:3000 | Implemented |

## 6. Recommendations for Production

| Priority | Recommendation |
|----------|---------------|
| High | Enable HTTPS with valid TLS certificate |
| High | Add HSTS header |
| High | Encrypt file storage at rest |
| High | Rate-limit all endpoints (not just auth) |
| Medium | Add CSP, X-Frame-Options, X-Content-Type-Options headers |
| Medium | Implement request body size limits on all endpoints |
| Medium | Move to database storage with parameterised queries |
| Medium | Add anti-automation (CAPTCHA) on registration |
| Low | Implement IP allowlisting for admin endpoints |
| Low | Add log integrity verification (signed log entries) |
| Low | Consider short-lived sessions with refresh tokens |
