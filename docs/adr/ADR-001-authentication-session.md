# ADR-001: Authentication and Session Approach

## Status
Accepted

## Context
TaskHub requires user authentication for a multi-tenant todo platform. The frontend is a React SPA communicating with an ASP.NET Core backend API. We need a secure, maintainable auth mechanism that protects against common web attacks.

## Decision
Use cookie-based session authentication via ASP.NET Core's built-in `CookieAuthenticationDefaults` scheme.

## Options Considered

### Option A: JWT Bearer Tokens
- Stateless, scalable
- Requires client-side token storage (localStorage/sessionStorage)
- Vulnerable to XSS if stored in localStorage
- Complex refresh token management needed

### Option B: Cookie-Based Sessions (Chosen)
- HttpOnly cookies prevent JavaScript access (XSS mitigation)
- SameSite=Strict provides CSRF protection
- Built-in ASP.NET Core support with sliding expiration
- Server-side session management

### Option C: OAuth 2.0 / OpenID Connect
- Overkill for a single-application scenario
- Adds external dependency for an identity provider

## Consequences
- Cookies must be configured with HttpOnly, Secure, and SameSite=Strict
- API must handle 401/403 responses instead of redirects (overridden via cookie events)
- Frontend uses `withCredentials: true` on all API calls
- CORS must be tightly configured to allow credentials only from the frontend origin

## Follow-ups
- Consider token-based auth if mobile clients are added
- Evaluate ASP.NET Core Identity for more advanced user management
- Add refresh/re-authentication flow for sensitive operations
