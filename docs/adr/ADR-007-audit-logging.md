# ADR-007: Audit Logging Design

## Status
Accepted

## Context
TaskHub requires audit trails for security, compliance, and debugging. Important events (auth, todo lifecycle, org changes) must be recorded with actor, timestamp, and traceability context.

## Decision
Implement an application-level `AuditService` that records events to the storage layer. Each entry includes timestamp, actorUserId, orgId, actionType, entityType, entityId, details, and correlationId.

## Options Considered

### Option A: Database Triggers / Storage-Level Auditing
- Automatic but tightly coupled to storage implementation
- Doesn't work with in-memory storage

### Option B: Application-Level Audit Service (Chosen)
- Explicit audit calls in service methods
- Works identically across all storage providers
- Full control over what is logged and the audit entry structure
- CorrelationId linked to the HTTP request

### Option C: Event Sourcing
- Complete history but massive complexity increase
- Overkill for the current requirements

## Consequences
- Audit entries are write-once (no update/delete)
- Events: LoginSuccess, LoginFailed, Logout, TodoCreated, TodoUpdated, TodoToggled, TodoSoftDeleted, TodoRestored, TodoHardDeleted, TodosExported, TodosImported
- OrgAdmin-only access to audit logs, enforced by authorization policy
- Correlation ID links audit entries to specific HTTP requests

## Follow-ups
- Add org membership events (member added/removed, role changes)
- Add audit log retention/archival policy
- Consider streaming audit events to external SIEM
