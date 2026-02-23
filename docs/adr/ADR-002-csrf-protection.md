# ADR-002: CSRF Protection Approach

## Status
Accepted

## Context
Cookie-based authentication is vulnerable to CSRF attacks where a malicious site triggers requests using the user's cookies. We need explicit CSRF protection for all state-changing operations.

## Decision
Rely on SameSite=Strict cookie policy combined with CORS restrictions as the primary CSRF defense. No anti-forgery tokens.

## Options Considered

### Option A: Anti-Forgery Tokens (Double Submit Cookie)
- Traditional ASP.NET approach
- Adds complexity to every form and AJAX call
- Requires server-side token generation and validation

### Option B: SameSite=Strict + CORS (Chosen)
- SameSite=Strict prevents the browser from sending cookies on any cross-origin request
- CORS restricted to `http://localhost:3000` blocks cross-origin AJAX
- Simpler implementation, fewer moving parts
- Well-supported in modern browsers (>95% support)

### Option C: Custom Header Requirement
- Require a custom header (e.g., X-Requested-With) on all requests
- Simple but less robust than SameSite

## Consequences
- No separate CSRF tokens needed, reducing frontend complexity
- Protection depends on browser SameSite support (all modern browsers)
- CORS origin must be updated for production deployments
- Referenced: OWASP CSRF Prevention Cheat Sheet, SameSite cookies guidance

## Follow-ups
- Add anti-forgery tokens if SameSite browser support becomes a concern
- Review CORS origins when deploying to production
