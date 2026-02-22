# Data Model — TaskHub

## Entity Relationship Diagram

```
┌──────────────┐       ┌──────────────────┐       ┌──────────────┐
│    User      │       │   Membership     │       │ Organisation │
├──────────────┤       ├──────────────────┤       ├──────────────┤
│ Id (PK)      │──┐    │ Id (PK)          │   ┌──│ Id (PK)      │
│ Username     │  └───▶│ UserId (FK)      │   │  │ Name         │
│ Email        │       │ OrganisationId(FK│◀──┘  │ CreatedAt    │
│ PasswordHash │       │ Role             │      │ CreatedBy(FK)│
│ CreatedAt    │       │ JoinedAt         │      └──────┬───────┘
│ LastLoginAt  │       └──────────────────┘             │
│ FailedLogin  │                                        │
│  Attempts    │                                   ┌────┴───────┐
│ LockoutEnd   │                                   │            │
└──────────────┘                              ┌────▼────┐  ┌────▼────┐
                                              │  Todo   │  │AuditLog │
                                              ├─────────┤  ├─────────┤
                                              │ Id (PK) │  │ Id (PK) │
                                              │ OrgId   │  │ OrgId   │
                                              │ CreatedBy│  │ ActorId │
                                              │ Title   │  │ Action  │
                                              │ Desc    │  │ Entity  │
                                              │ Status  │  │ Type    │
                                              │ Priority│  │ EntityId│
                                              │ Tags    │  │ Details │
                                              │ DueDate │  │ CorrId  │
                                              │ Created │  │ Timestamp│
                                              │ Updated │  └─────────┘
                                              │ Deleted │
                                              │ Version │
                                              └─────────┘
```

## Entities

### User

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| Username | string | Required, 3–50 chars, unique | Login name |
| Email | string | Required, valid email, unique | Email address |
| PasswordHash | string | Required | BCrypt hash |
| CreatedAt | DateTime | Default: UTC now | Registration timestamp |
| LastLoginAt | DateTime? | Nullable | Last successful login |
| FailedLoginAttempts | int | Default: 0 | Failed login counter |
| LockoutEnd | DateTime? | Nullable | Lockout expiry time |

**Computed:** `IsLocked` = `LockoutEnd > DateTime.UtcNow`

**Relationships:**
- Has many Memberships (1:N)

---

### Organisation

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| Name | string | Required, 3–100 chars | Organisation name |
| CreatedAt | DateTime | Default: UTC now | Creation timestamp |
| CreatedBy | Guid | FK → User.Id | Creator (becomes OrgAdmin) |

**Relationships:**
- Has many Memberships (1:N)
- Has many Todos (1:N)
- Has many AuditLogs (1:N)

---

### Membership

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| UserId | Guid | FK → User.Id | Team member |
| OrganisationId | Guid | FK → Organisation.Id | Organisation |
| Role | Role enum | Member or OrgAdmin | Access level |
| JoinedAt | DateTime | Default: UTC now | Membership start |

**Unique Constraint:** (UserId, OrganisationId) — a user can be a member of an organisation only once

**Relationships:**
- Belongs to User (N:1)
- Belongs to Organisation (N:1)

---

### Todo

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| OrganisationId | Guid | FK → Organisation.Id | Owning organisation |
| CreatedBy | Guid | FK → User.Id | Creator |
| Title | string | Required, 3–200 chars | Task title |
| Description | string? | Max 1000 chars | Detailed description |
| Status | TodoStatus | Open or Done, default Open | Completion status |
| Priority | Priority | Low/Medium/High, default Medium | Priority level |
| Tags | List\<string\> | Each max 50 chars, alphanumeric+hyphen+underscore | Categorisation |
| DueDate | DateTime? | Must be future on create | Deadline |
| CreatedAt | DateTime | Default: UTC now | Creation timestamp |
| UpdatedAt | DateTime | Updated on every mutation | Last modified |
| DeletedAt | DateTime? | Null = active | Soft delete marker |
| Version | string | Auto-generated GUID | Optimistic concurrency token |

**Relationships:**
- Belongs to Organisation (N:1)
- Belongs to User (N:1) via CreatedBy

---

### AuditLog

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Log entry ID |
| Timestamp | DateTime | Default: UTC now | Event time |
| ActorUserId | Guid | FK → User.Id | Who performed the action |
| OrganisationId | Guid? | FK → Organisation.Id | Affected organisation |
| ActionType | string | Required | Action name |
| EntityType | string | Required | Entity category |
| EntityId | string | Required | Affected entity ID |
| Details | string? | Optional | Additional context |
| CorrelationId | string | Required | Request trace ID |

**Relationships:**
- Belongs to User (N:1) via ActorUserId
- Belongs to Organisation (N:1) via OrganisationId

**ActionTypes:**
- TodoCreated, TodoUpdated, TodoDeleted, TodoRestored, TodoHardDeleted
- LoginFailed, Logout
- MemberAdded, MemberRemoved, RoleChanged

---

## Enumerations

### TodoStatus
| Value | Numeric | Description |
|-------|---------|-------------|
| Open | 0 | Not completed |
| Done | 1 | Completed |

### Priority
| Value | Numeric | Description |
|-------|---------|-------------|
| Low | 0 | Low priority |
| Medium | 1 | Normal priority |
| High | 2 | Urgent |

### Role
| Value | Numeric | Description |
|-------|---------|-------------|
| Member | 0 | Standard access |
| OrgAdmin | 1 | Administrative access |

---

## Import/Export Model

### TodoExportModel

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| ClientProvidedId | string? | null | External ID for idempotency |
| Title | string | "" | Task title |
| Description | string? | null | Description |
| Status | string | "Open" | String representation of TodoStatus |
| Priority | string | "Medium" | String representation of Priority |
| Tags | List\<string\> | [] | Tags list |
| DueDate | DateTime? | null | Due date |

---

## File Storage Schema

When using FileStorage, all data is persisted in a single JSON file:

```json
{
  "schemaVersion": 2,
  "lastModified": "2026-02-22T10:00:00Z",
  "users": { "<guid>": { ... } },
  "organisations": { "<guid>": { ... } },
  "memberships": { "<userId>:<orgId>": { ... } },
  "todos": { "<guid>": { ... } },
  "auditLogs": { "<guid>": { ... } }
}
```

### Schema Migrations

| From | To | Changes |
|------|----|---------|
| 0 | 1 | Added `Version` field to all todos (for ETag support) |
| 1 | 2 | Added `Description` field to todos (default empty string) |
