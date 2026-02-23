# Operations Runbook — TaskHub

## 1. Prerequisites

- .NET 9 SDK
- Node.js 18+ and npm
- Git

## 2. Starting the Application

### Backend

```bash
cd backend
dotnet run --project src/TaskHub.Api
```

Default: `https://localhost:5001` / `http://localhost:5000`

### Frontend

```bash
cd frontend
npm install
npm start
```

Default: `http://localhost:3000`

### Verify Health

```bash
curl http://localhost:5000/health/live    # Should return "OK"
curl http://localhost:5000/health/ready   # Should return "OK"
```

## 3. Configuration

### Storage Provider

In `appsettings.json`:

```json
{
  "StorageProvider": "InMemory"
}
```

Options:
- `"InMemory"` — Volatile storage (default, for development)
- `"File"` — JSON file persistence

### File Storage Path

```json
{
  "FileStorage": {
    "Path": "storage"
  }
}
```

Data file: `storage/taskhub-data.json`

### Rate Limiting

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      { "Endpoint": "POST:/api/v1/auth/login", "Period": "1m", "Limit": 5 },
      { "Endpoint": "POST:/api/v1/auth/register", "Period": "1h", "Limit": 10 }
    ]
  }
}
```

### Logging

Serilog configuration in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

Log files: `logs/taskhub-YYYYMMDD.txt` (rolling daily)

## 4. Common Operations

### Check Application Status

```bash
# Liveness
curl -s http://localhost:5000/health/live

# Readiness (checks storage)
curl -s http://localhost:5000/health/ready

# Swagger UI
open http://localhost:5000/swagger
```

### View Logs

```bash
# Tail the latest log file
tail -f logs/taskhub-$(date +%Y%m%d).txt

# Search for errors
grep -i "error\|exception" logs/taskhub-*.txt
```

### Run Tests

```bash
cd backend
dotnet test TaskHub.sln
```

### Build

```bash
# Backend
cd backend
dotnet build TaskHub.sln

# Frontend
cd frontend
npm run build
```

### Export Data (via API)

```bash
# JSON export (requires auth cookie)
curl -b cookies.txt http://localhost:5000/api/v1/importexport/export?format=json -o backup.json

# CSV export
curl -b cookies.txt http://localhost:5000/api/v1/importexport/export?format=csv -o backup.csv
```

### Import Data (via API)

```bash
# JSON import from file
curl -b cookies.txt -F "file=@todos.json" \
  "http://localhost:5000/api/v1/importexport/import?format=json"
```

## 5. Troubleshooting

### Application Won't Start

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Port already in use` | Another process on 5000/5001 | Kill process or change port in `launchSettings.json` |
| `FileNotFoundException` for storage | FileStorage path doesn't exist | Create `storage/` directory |
| `dotnet: command not found` | .NET SDK not installed | Install .NET 9 SDK |

### Authentication Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| 401 on all requests | Cookie expired or not sent | Re-login; check cookie domain/path |
| "Account is locked" | 5+ failed login attempts | Wait 15 minutes or restart (InMemory) |
| Cookie not set in browser | HTTPS mismatch | Use HTTPS or disable Secure cookie in dev |

### Data Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| Data lost after restart | Using InMemoryStorage | Switch to `"StorageProvider": "File"` |
| `taskhub-data.json` corrupt | Interrupted write | Delete file and restart (data loss) |
| Schema version mismatch | Old data file with new code | MigrationService auto-migrates on load |
| Import returns 0 accepted | Case-sensitive JSON mismatch | Ensure camelCase or PascalCase (both supported) |

### Performance Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| Slow responses with FileStorage | Large data file | Consider migrating to database; check SemaphoreSlim contention |
| High memory usage | Large InMemory dataset | Restart; consider FileStorage |
| 429 Too Many Requests | Rate limit hit | Wait for rate limit window to reset |

## 6. Monitoring Checklist

| Check | Frequency | Method |
|-------|-----------|--------|
| Health endpoints | Continuous | `GET /health/live` and `/health/ready` |
| Log file errors | Daily | `grep -i error logs/taskhub-*.txt` |
| Disk space | Weekly | Check `storage/` and `logs/` directory sizes |
| Dependency vulnerabilities | Monthly | `dotnet list package --vulnerable` |
| Test suite | On every change | `dotnet test` |

## 7. Backup & Recovery

### Backup (FileStorage)

```bash
# Copy data file
cp storage/taskhub-data.json backups/taskhub-data-$(date +%Y%m%d).json
```

### Restore

```bash
# Stop application first
cp backups/taskhub-data-YYYYMMDD.json storage/taskhub-data.json
# Restart application
```

### Export-based Backup

```bash
# Per-organisation export via API
curl -b cookies.txt \
  -H "X-Organisation-Id: <org-id>" \
  http://localhost:5000/api/v1/importexport/export?format=json \
  -o org-backup.json
```

## 8. Shutdown

```bash
# Graceful shutdown
# Ctrl+C in the terminal running the application
# Or: kill -SIGTERM <pid>
```

The application handles SIGTERM gracefully — in-flight requests complete, SemaphoreSlim releases, and file handles close.
