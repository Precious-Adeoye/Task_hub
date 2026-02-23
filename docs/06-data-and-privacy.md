# Data & Privacy — TaskHub

## 1. Data Classification

| Data Type | Classification | Storage | Retention |
|-----------|---------------|---------|-----------|
| Passwords | Sensitive | BCrypt hash only; plaintext never stored | Indefinite |
| Email addresses | PII | In user record | Until account deletion |
| Usernames | PII | In user record | Until account deletion |
| Session cookies | Sensitive | Client-side (HttpOnly) | 7-day sliding expiration |
| Todo content | Business | Organisation-scoped | Until hard-deleted |
| Audit logs | Compliance | Organisation-scoped | Indefinite |
| Correlation IDs | Operational | In audit logs + log files | Log file rotation policy |
| IP addresses | PII | In rate limiter memory only | Request lifetime only |

## 2. Data at Rest

### InMemory Storage
- Data held in ConcurrentDictionary instances in application memory
- Data is **not persisted** — lost on process restart
- Suitable for development and testing only

### File Storage
- Single JSON file: `storage/taskhub-data.json`
- **Not encrypted** — relies on filesystem permissions
- Atomic writes via temp file + rename pattern
- Schema version tracked in file header

### Recommendation for Production
- Encrypt the storage file at rest or migrate to a database with TDE (Transparent Data Encryption)
- Apply restrictive file permissions (owner-only read/write)

## 3. Data in Transit

| Path | Protection |
|------|-----------|
| Browser → API | HTTPS enforced via `UseHttpsRedirection()` |
| Cookie transmission | `Secure=true` (HTTPS only), `SameSite=Strict`, `HttpOnly=true` |
| API → Storage | In-process (no network hop) |
| Log output | Local file only; no remote transmission |

## 4. Authentication & Session Security

| Control | Implementation |
|---------|---------------|
| Password hashing | BCrypt with default work factor |
| Session management | ASP.NET cookie authentication |
| Cookie security | HttpOnly, Secure, SameSite=Strict |
| Session duration | 7 days with sliding expiration |
| Brute-force protection | 5-attempt lockout (15 min), IP rate limiting |
| Credential enumeration | Generic error message on registration failure |

## 5. Access Control

| Resource | Member | OrgAdmin |
|----------|--------|----------|
| Own todos (CRUD) | Yes | Yes |
| Soft delete / restore | Yes | Yes |
| Hard delete (permanent) | No | Yes |
| View members list | No | Yes |
| Add / remove members | No | Yes |
| Change member roles | No | Yes (not own role) |
| View audit logs | No | Yes |
| Import / export | Yes | Yes |

## 6. Multi-Tenancy Isolation

- Every data query is scoped by `OrganisationId`
- Organisation context is resolved from `X-Organisation-Id` header
- `RequireOrganisation` attribute verifies membership before any org-scoped action
- Users can only access data within organisations they belong to
- No cross-organisation data leakage is possible through the API

## 7. Audit Trail

All data mutations are recorded with:
- **Timestamp** — UTC
- **Actor** — User ID of the person performing the action
- **Action type** — TodoCreated, TodoUpdated, TodoDeleted, TodoRestored, TodoHardDeleted, LoginFailed, Logout, MemberAdded, MemberRemoved, RoleChanged
- **Entity reference** — Type and ID of affected entity
- **Correlation ID** — Links audit entry to the HTTP request

Audit logs are immutable — there is no API to modify or delete them.

## 8. Data Minimisation

- No unnecessary PII collected (no phone numbers, addresses, etc.)
- IP addresses are used only for rate limiting and not persisted
- Failed login attempts are counted but no details about the attempt are stored beyond the count
- Audit logs record user IDs, not usernames/emails

## 9. Data Portability

Users and admins can export all todo data via:
- `GET /api/v1/importexport/export?format=json` — structured JSON
- `GET /api/v1/importexport/export?format=csv` — tabular CSV

Exported data includes: ClientProvidedId, Title, Description, Status, Priority, Tags, DueDate.

## 10. Data Deletion

| Operation | Scope | Reversible | Who |
|-----------|-------|-----------|-----|
| Soft delete | Single todo | Yes (restore) | Any member |
| Hard delete | Single todo | No | OrgAdmin only |
| Account deletion | Not implemented | — | — |
| Organisation deletion | Not implemented | — | — |

### Future Considerations
- Implement account deletion with cascading data removal
- Add organisation deletion with member notification
- Consider GDPR right-to-erasure compliance for EU users
- Add data retention policies with automated cleanup
