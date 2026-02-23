# ADR-006: API Error Model (Problem Details)

## Status
Accepted

## Context
API errors must be consistent, machine-readable, and include enough context for debugging without leaking sensitive internals. The industry standard is RFC 7807 / RFC 9457 (Problem Details for HTTP APIs).

## Decision
Use Problem Details format for all API error responses via custom `ProblemDetailsMiddleware`. Include `correlationId` in every error response.

## Options Considered

### Option A: Custom Error JSON Format
- Flexible but non-standard
- Every consumer must learn the custom format

### Option B: RFC 7807 Problem Details (Chosen)
- Industry standard, well-supported by tooling
- Content-Type: `application/problem+json`
- Fields: type, title, status, detail, instance
- Extensions for correlationId and validation errors

### Option C: ASP.NET Built-in Problem Details
- Limited customization
- Doesn't include correlationId by default

## Consequences
- All exceptions caught by middleware and mapped to Problem Details
- ValidationException -> 400 with field-level error details
- UnauthorizedAccessException -> 401
- KeyNotFoundException -> 404
- Stack traces only included in Development environment
- CorrelationId always included for request tracing

## Follow-ups
- Add error type URIs pointing to documentation
- Consider error catalog for common error codes
