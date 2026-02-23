# ADR-005: Concurrency Strategy (ETag/Version)

## Status
Accepted

## Context
Multiple users may edit the same todo concurrently. Without concurrency control, the last write wins and earlier changes are silently lost. We need optimistic concurrency control per RFC 9110 (HTTP Semantics).

## Decision
Use a GUID-based `version` field on Todo entities, exposed as HTTP `ETag` headers. Require `If-Match` on update/delete/restore operations. Return `412 Precondition Failed` on version mismatch.

## Options Considered

### Option A: Pessimistic Locking
- Lock the resource during edit
- Poor UX for web applications, complex timeout management

### Option B: Last-Write-Wins
- No conflict detection
- Data loss when concurrent edits occur

### Option C: Optimistic Concurrency with ETag/If-Match (Chosen)
- Version field updated on every write
- ETag header returned with every response
- If-Match required on mutations
- 412 status on mismatch signals conflict to the client
- Aligns with HTTP semantics (RFC 9110 Section 13.1)

## Consequences
- Every todo mutation generates a new version GUID
- Frontend must capture and send ETag/version on updates
- ETagMiddleware also generates content-based ETags for GET responses (conditional GETs with If-None-Match -> 304)
- Tested with automated concurrency conflict test

## Follow-ups
- Add conflict resolution UI (show both versions, allow merge)
- Consider content-based versioning (hash) instead of random GUID
