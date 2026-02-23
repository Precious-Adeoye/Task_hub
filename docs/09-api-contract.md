# API Contract — TaskHub

Base URL: `/api/v1`
Content-Type: `application/json`
Authentication: Cookie-based (HttpOnly, Secure, SameSite=Strict)

## Authentication

### POST /auth/register

Register a new user account.

**Request:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass1!"
}
```

**Validation:**
- `username`: 3–50 chars, alphanumeric + underscore
- `email`: valid email, max 100 chars
- `password`: min 8 chars, must contain uppercase, lowercase, digit, special char

**Response 200:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "email": "john@example.com"
}
```

**Response 400:** ProblemDetails — validation errors or generic registration failure

---

### POST /auth/login

Authenticate and receive session cookie.

**Request:**
```json
{
  "username": "john_doe",
  "password": "SecurePass1!"
}
```

**Response 200:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "email": "john@example.com"
}
```

**Response Headers:** `Set-Cookie: .AspNetCore.Cookies=...; HttpOnly; Secure; SameSite=Strict; Path=/`

**Response 401:** ProblemDetails — "Invalid credentials" or "Account is locked"

---

### POST /auth/logout

End the current session.

**Headers Required:** Valid session cookie

**Response 200:**
```json
{ "message": "Logged out successfully" }
```

---

### GET /auth/me

Get the current authenticated user.

**Headers Required:** Valid session cookie

**Response 200:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "email": "john@example.com"
}
```

**Response 401:** ProblemDetails — "Unauthorized"

---

## Todos

All todo endpoints require authentication and `X-Organisation-Id` header.

### GET /todo

List todos with filtering, sorting, and pagination.

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| status | string | — | Filter: "Open" or "Done" |
| overdue | bool | — | Filter overdue items |
| tag | string | — | Filter by tag |
| includeDeleted | bool | false | Include soft-deleted |
| page | int | 1 | Page number |
| pageSize | int | 20 | Items per page |
| sortBy | string | "createdAt" | Sort field: createdAt, dueDate, priority |
| sortDescending | bool | true | Sort direction |

**Response 200:**
```json
[
  {
    "id": "...",
    "title": "Buy groceries",
    "description": "Milk, eggs, bread",
    "status": "Open",
    "priority": "High",
    "tags": ["shopping", "personal"],
    "dueDate": "2026-03-01T00:00:00Z",
    "createdAt": "2026-02-20T10:30:00Z",
    "updatedAt": "2026-02-20T10:30:00Z",
    "deletedAt": null,
    "version": "abc-123"
  }
]
```

**Response Headers:**
- `X-Total-Count: 42`

---

### GET /todo/{id}

Get a single todo by ID.

**Response 200:** TodoResponse (see above)
**Response Headers:** `ETag: "abc-123"`
**Response 404:** ProblemDetails — "Todo not found"

---

### POST /todo

Create a new todo.

**Request:**
```json
{
  "title": "Buy groceries",
  "description": "Milk, eggs, bread",
  "priority": "High",
  "tags": ["shopping"],
  "dueDate": "2026-03-01T00:00:00Z"
}
```

**Validation:**
- `title`: 3–200 chars, required
- `description`: max 1000 chars
- `tags`: each max 50 chars, alphanumeric + hyphen/underscore
- `dueDate`: must be in the future
- `priority`: Low, Medium, or High (enum, default Medium)

**Response 201:** TodoResponse with Location header
**Response 400:** ProblemDetails — validation errors

---

### PUT /todo/{id}

Update a todo. Requires If-Match header for concurrency control.

**Headers Required:**
- `If-Match: "abc-123"` (current ETag)

**Request:**
```json
{
  "title": "Updated title",
  "description": "Updated description",
  "priority": "Low",
  "tags": ["updated"],
  "dueDate": "2026-04-01T00:00:00Z"
}
```

All fields are optional — only provided fields are updated.

**Response 200:** Updated TodoResponse with new ETag
**Response 412:** ProblemDetails — "Precondition Failed" (version mismatch)
**Response 404:** ProblemDetails — "Todo not found"

---

### PATCH /todo/{id}/toggle

Toggle todo status between Open and Done.

**Response 200:** Updated TodoResponse
**Response 404:** ProblemDetails

---

### DELETE /todo/{id}/soft

Soft-delete a todo (sets DeletedAt timestamp).

**Headers Required:** `If-Match: "abc-123"`

**Response 200:** `{ "message": "Todo soft-deleted" }`
**Response 412:** ProblemDetails — version mismatch
**Response 404:** ProblemDetails

---

### POST /todo/{id}/restore

Restore a soft-deleted todo.

**Headers Required:** `If-Match: "abc-123"`

**Response 200:** Restored TodoResponse
**Response 412:** ProblemDetails — version mismatch
**Response 404:** ProblemDetails

---

### DELETE /todo/{id}

Hard-delete a todo (permanent, admin only).

**Headers Required:** `If-Match: "abc-123"`
**Authorization:** OrgAdmin

**Response 200:** `{ "message": "Todo permanently deleted" }`
**Response 412:** ProblemDetails — version mismatch
**Response 403:** ProblemDetails — insufficient permissions

---

## Organisations

### POST /organisations

Create a new organisation. Creator becomes OrgAdmin.

**Request:**
```json
{ "name": "My Team" }
```

**Validation:** `name`: 3–100 chars, alphanumeric + spaces/hyphens/underscores

**Response 201:** OrganisationResponse

---

### GET /organisations

List the current user's organisations.

**Response 200:**
```json
[
  {
    "id": "...",
    "name": "My Team",
    "createdAt": "2026-02-20T10:00:00Z",
    "createdBy": "..."
  }
]
```

---

### GET /organisations/{id}

Get organisation details. Requires membership.

**Response 200:** OrganisationResponse
**Response 403:** ProblemDetails — not a member

---

### GET /organisations/{id}/members

List organisation members. Admin only.

**Response 200:**
```json
[
  {
    "userId": "...",
    "username": "john_doe",
    "email": "john@example.com",
    "role": "OrgAdmin",
    "joinedAt": "2026-02-20T10:00:00Z"
  }
]
```

---

### POST /organisations/{id}/members

Add a member by email. Admin only.

**Request:**
```json
{
  "email": "newmember@example.com",
  "role": "Member"
}
```

**Response 200:** `{ "message": "Member added" }`
**Response 400:** ProblemDetails — user not found or already a member

---

### PUT /organisations/{id}/members/{userId}/role

Change a member's role. Admin only. Cannot change own role.

**Request:**
```json
{ "role": "OrgAdmin" }
```

**Response 200:** `{ "message": "Role updated" }`

---

### DELETE /organisations/{id}/members/{userId}

Remove a member. Admin only.

**Response 200:** `{ "message": "Member removed" }`

---

## Audit

All audit endpoints require OrgAdmin role.

### GET /audit

List audit logs with pagination and date filtering.

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| from | DateTime | — | Start date |
| to | DateTime | — | End date |
| page | int | 1 | Page number |
| pageSize | int | 50 | Items per page |

**Response 200:**
```json
{
  "logs": [
    {
      "id": "...",
      "timestamp": "2026-02-20T10:30:00Z",
      "actorUserId": "...",
      "actionType": "TodoCreated",
      "entityType": "Todo",
      "entityId": "...",
      "details": "Created todo: Buy groceries",
      "correlationId": "req-abc-123"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 50
}
```

---

### GET /audit/summary

Audit summary grouped by action type.

**Query Parameters:** `from`, `to` (DateTime)

**Response 200:**
```json
{
  "from": "2026-02-01T00:00:00Z",
  "to": "2026-02-22T00:00:00Z",
  "actions": [
    { "actionType": "TodoCreated", "count": 45, "lastOccurrence": "2026-02-22T10:00:00Z" },
    { "actionType": "TodoUpdated", "count": 30, "lastOccurrence": "2026-02-22T09:00:00Z" }
  ]
}
```

---

## Import / Export

### GET /importexport/export

Export todos as JSON or CSV file.

**Query Parameters:** `format` — "json" (default) or "csv"

**Response 200:** File download
- JSON: `Content-Type: application/json`, filename `todos-export.json`
- CSV: `Content-Type: text/csv`, filename `todos-export.csv`

---

### POST /importexport/import

Import todos from file upload (multipart/form-data).

**Query Parameters:**
- `format`: "json" or "csv"
- `idempotent`: bool (default true)
- `overwrite`: bool (default false)

**Request:** `multipart/form-data` with `file` field (max 10 MB)

**Response 200:**
```json
{
  "acceptedCount": 8,
  "rejectedCount": 2,
  "errors": [
    { "rowNumber": 3, "clientProvidedId": "row-3", "errorMessage": "Title is required" },
    { "rowNumber": 7, "clientProvidedId": "row-7", "errorMessage": "Invalid status: Pending" }
  ]
}
```

---

### POST /importexport/import/json

Import todos from JSON body (no file upload).

**Request:** Array of TodoExportModel
```json
[
  {
    "clientProvidedId": "todo-1",
    "title": "Imported Todo",
    "status": "Open",
    "priority": "High",
    "tags": ["imported"],
    "dueDate": "2026-03-01T00:00:00Z"
  }
]
```

**Response 200:** ImportResult (see above)

---

### GET /importexport/template

Download import template file. No authentication required.

**Query Parameters:** `format` — "json" (default) or "csv"

**Response 200:** Template file download

---

## Health

### GET /health/live

Basic liveness check. No authentication required.

**Response 200:** `"OK"`

---

### GET /health/ready

Readiness check (verifies storage availability).

**Response 200:** `"OK"`
**Response 503:** Storage unavailable

---

## Common Headers

| Header | Direction | Description |
|--------|-----------|-------------|
| X-Organisation-Id | Request | Tenant context for org-scoped endpoints |
| X-Correlation-ID | Request/Response | Request tracing ID (auto-generated if missing) |
| If-Match | Request | ETag for optimistic concurrency on mutations |
| ETag | Response | Entity version on GET responses |
| X-Total-Count | Response | Total item count for paginated lists |

## Common Error Format (RFC 7807)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Title must be between 3 and 200 characters",
  "instance": "/api/v1/todo"
}
```
