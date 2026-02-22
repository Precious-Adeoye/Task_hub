# Requirements — TaskHub

## 1. Personas

### Persona 1: Alex — Team Lead (OrgAdmin)

- **Role:** Organisation administrator
- **Goals:** Create organisations, invite team members, assign roles, review audit logs, import/export data
- **Pain points:** Needs visibility into who changed what and when; wants to onboard new team members quickly
- **Typical workflow:**
  1. Register → Create organisation → Invite members by email
  2. Review audit logs weekly for compliance
  3. Export todos monthly for reporting
  4. Hard-delete obsolete items to keep the workspace clean

### Persona 2: Sam — Developer (Member)

- **Role:** Organisation member
- **Goals:** Create, update, and complete todos; filter by status/priority/tag; track due dates
- **Pain points:** Doesn't want to lose work to concurrent edits; needs clear feedback on conflicts
- **Typical workflow:**
  1. Login → Select organisation → View open todos sorted by due date
  2. Create new todos with tags and priority
  3. Toggle status when work is done
  4. Edit todo details; handle 412 conflicts by refreshing

### Persona 3: Jordan — External Auditor (Read-Only Admin)

- **Role:** OrgAdmin granted for audit purposes
- **Goals:** Review audit logs, verify data integrity, export records
- **Pain points:** Needs time-filtered views; needs downloadable evidence
- **Typical workflow:**
  1. Login → Select organisation → Open audit panel
  2. Filter logs by date range
  3. Export todos as CSV for offline analysis

## 2. Functional Requirements

### FR-1: Authentication

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1.1 | Users can register with username, email, and password | Must |
| FR-1.2 | Passwords must be ≥ 8 chars with uppercase, lowercase, digit, and special character | Must |
| FR-1.3 | Passwords are hashed with BCrypt before storage | Must |
| FR-1.4 | Users can login with username or email | Must |
| FR-1.5 | Session is cookie-based (HttpOnly, Secure, SameSite=Strict) | Must |
| FR-1.6 | Account locks after 5 failed login attempts for 15 minutes | Must |
| FR-1.7 | Users can logout (cookie cleared) | Must |
| FR-1.8 | GET /me returns current user info | Should |

### FR-2: Organisation Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-2.1 | Authenticated users can create organisations | Must |
| FR-2.2 | Creator automatically becomes OrgAdmin | Must |
| FR-2.3 | OrgAdmins can add members by email | Must |
| FR-2.4 | OrgAdmins can remove members | Must |
| FR-2.5 | OrgAdmins can change member roles (Member ↔ OrgAdmin) | Must |
| FR-2.6 | Users cannot modify their own role | Must |
| FR-2.7 | Users can list their organisations | Must |
| FR-2.8 | Data is scoped to the selected organisation | Must |

### FR-3: Todo Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-3.1 | Members can create todos (title required, description/priority/tags/dueDate optional) | Must |
| FR-3.2 | Members can list todos with pagination (default 20/page) | Must |
| FR-3.3 | Members can filter by status, overdue flag, tag | Must |
| FR-3.4 | Members can sort by createdAt, dueDate, priority | Must |
| FR-3.5 | Members can update todo fields | Must |
| FR-3.6 | Members can toggle status (Open ↔ Done) | Must |
| FR-3.7 | Members can soft-delete todos (sets DeletedAt) | Must |
| FR-3.8 | Members can restore soft-deleted todos | Must |
| FR-3.9 | OrgAdmins can hard-delete todos (permanent) | Must |
| FR-3.10 | Updates use ETag/If-Match; return 412 on version mismatch | Must |

### FR-4: Import / Export

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-4.1 | Export todos as JSON or CSV (excludes soft-deleted) | Must |
| FR-4.2 | Import from JSON body or file upload (10 MB limit) | Must |
| FR-4.3 | Import supports ClientProvidedId for idempotency | Must |
| FR-4.4 | Per-row validation with AcceptedCount/RejectedCount/Errors | Must |
| FR-4.5 | Import template download (JSON and CSV) | Should |

### FR-5: Audit Logging

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-5.1 | All mutations create audit log entries | Must |
| FR-5.2 | Login failures and logouts are logged | Must |
| FR-5.3 | Each request has a correlation ID (X-Correlation-ID) | Must |
| FR-5.4 | OrgAdmins can list audit logs with date filter + pagination | Must |
| FR-5.5 | OrgAdmins can view audit summary by action type | Should |

## 3. Non-Functional Requirements

| ID | Requirement | Category |
|----|-------------|----------|
| NFR-1 | API responds within 200ms for typical requests (InMemory) | Performance |
| NFR-2 | All error responses use RFC 7807 ProblemDetails format | Interoperability |
| NFR-3 | API versioned under /api/v1 prefix | Maintainability |
| NFR-4 | Input validated at API boundary (FluentValidation) | Security |
| NFR-5 | Structured logging with Serilog (console + rolling file) | Observability |
| NFR-6 | Health endpoints available without authentication | Operations |
| NFR-7 | Swagger/OpenAPI documentation auto-generated | Developer Experience |
| NFR-8 | Storage backend switchable via configuration | Flexibility |
| NFR-9 | File storage uses atomic writes (temp + rename) | Reliability |
| NFR-10 | Schema versioning with forward migration | Maintainability |

## 4. Failure Paths & Error Handling

### FP-1: Authentication Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Invalid credentials | Wrong password | ProblemDetails: "Invalid credentials" | 401 |
| Account locked | 5+ failed attempts | ProblemDetails: "Account locked" | 401 |
| Not authenticated | Missing/expired cookie | ProblemDetails: "Unauthorized" | 401 |
| Duplicate registration | Email/username taken | ProblemDetails: generic message (prevent enumeration) | 400 |
| Weak password | Fails validation rules | ProblemDetails: validation errors | 400 |

### FP-2: Authorisation Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Not in organisation | User not a member of the org | ProblemDetails: "Forbidden" | 403 |
| Not admin | Member tries admin-only action | ProblemDetails: "Forbidden" | 403 |
| Missing org header | No X-Organisation-Id | ProblemDetails: "Organisation context required" | 400 |

### FP-3: Concurrency Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Stale version | If-Match doesn't match current Version | ProblemDetails: "Precondition Failed" | 412 |
| Missing If-Match | Update/delete without If-Match header | ProblemDetails: "If-Match header required" | 412 |

### FP-4: Validation Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Title too short | < 3 characters | ProblemDetails: validation errors | 400 |
| Title too long | > 200 characters | ProblemDetails: validation errors | 400 |
| Invalid tag chars | Non-alphanumeric (except -_) | ProblemDetails: validation errors | 400 |
| Due date in past | DueDate < now | ProblemDetails: validation errors | 400 |
| Invalid priority | Not Low/Medium/High | ProblemDetails: validation errors | 400 |

### FP-5: Resource Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Todo not found | Invalid or wrong-org ID | ProblemDetails: "Not Found" | 404 |
| Organisation not found | Invalid org ID | ProblemDetails: "Not Found" | 404 |
| Member not found | Invalid user for removal | ProblemDetails: "Not Found" | 404 |

### FP-6: Import Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Invalid JSON | Malformed input | ProblemDetails: "Import failed" | 400 |
| File too large | > 10 MB | ProblemDetails: "File too large" | 400 |
| Row validation error | Missing title, bad status | ImportResult with per-row errors | 200 |

### FP-7: Rate Limiting

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| Login rate exceeded | > 5 requests/min from same IP | "Too Many Requests" | 429 |
| Register rate exceeded | > 10 requests/hr from same IP | "Too Many Requests" | 429 |

### FP-8: Storage Failures

| Scenario | Trigger | Response | HTTP Status |
|----------|---------|----------|-------------|
| File read error | Corrupted/missing data file | ProblemDetails: "Internal Server Error" | 500 |
| File write error | Disk full or permission denied | ProblemDetails: "Internal Server Error" | 500 |
| Health check fail | Storage unreachable | Unhealthy status at /health/ready | 503 |
