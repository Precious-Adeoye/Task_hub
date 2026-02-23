# Release Notes — TaskHub v0.2.0

## For Stakeholders

This release includes a critical bug fix, a new archiving feature, and security improvements to protect against automated login attacks.

### Bug Fix: Todo Status Toggle
Previously, rapidly toggling a todo's status could cause the UI to show an incorrect state until the page was refreshed. This has been fixed — the UI now updates immediately and rolls back gracefully if the server rejects the change.

### New Feature: Auto-Archive Completed Todos
Completed todos older than a configurable number of days are now automatically archived. Archived items no longer clutter the default todo list but remain accessible via the "Show archived" filter and can be restored at any time.

**Configuration:**
- `ARCHIVE_AFTER_DAYS`: Number of days after completion before archiving (default: 30)
- Archive job runs on a configurable schedule (default: daily)

### Security: Authentication Abuse Protection
Login and registration endpoints are now protected against scripted attacks with IP-based rate limiting:
- Login: 5 attempts per minute (configurable)
- Registration: 10 attempts per hour (configurable)
- Exceeding the limit returns HTTP 429 (Too Many Requests)
- Account lockout after 5 consecutive failed login attempts (15-minute lockout window)

These limits are intentionally lenient for legitimate users while blocking automated abuse.

---

## Technical Changelog

See [CHANGELOG.md](/CHANGELOG.md) for the full technical changelog.
