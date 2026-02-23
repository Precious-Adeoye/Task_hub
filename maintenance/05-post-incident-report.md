# Post-Incident Report: File Storage Corruption

## Incident Summary
- **Date**: 2026-02-20 (simulated)
- **Duration**: ~45 minutes
- **Severity**: High
- **Impact**: One organisation's todo data became unreadable after a server crash during a write operation

## Timeline
| Time | Event |
|------|-------|
| 14:00 | Server process crashed (out-of-memory) during a bulk import operation |
| 14:05 | Readiness health check began failing (`/health/ready` returned 503) |
| 14:10 | On-call engineer alerted via monitoring. Investigation started. |
| 14:15 | Root cause identified: storage JSON file contained a partial write (truncated JSON) |
| 14:20 | Confirmed that the atomic write mechanism (temp file + rename) was bypassed due to OOM during serialization |
| 14:30 | Restored data from the most recent backup (manual copy of storage directory) |
| 14:35 | Verified data integrity. Restarted application. Health checks passed. |
| 14:45 | Confirmed all users could access their data. Incident closed. |

## Root Cause
The file storage implementation writes data by:
1. Serializing to a temporary file
2. Renaming the temp file to the target (atomic on most filesystems)

However, if the process crashes during step 1 (serialization to temp file), the temp file contains partial data. On restart, the application attempted to read the original file, which was intact — **but** in this case, a previous rename had already replaced the original file with valid data, and the crash occurred during a subsequent write cycle where the original file had been successfully renamed away but the new temp file was incomplete.

## Contributing Factors
- No automatic backup before write operations
- Large import payload consumed excessive memory
- No file integrity check (e.g., checksum) on read

## Resolution
1. Restored from manual backup
2. Restarted the application process
3. Verified data via the health/readiness endpoint

## Corrective Actions

### Immediate (Completed)
- [x] Added pre-write backup: before atomic rename, copy existing file to `.bak`
- [x] Added JSON integrity check on file load (try-parse, fall back to `.bak` if corrupt)

### Short-Term (Planned)
- [ ] Add memory limit checks before processing large imports
- [ ] Implement streaming JSON serialization to reduce peak memory usage
- [ ] Add file checksum (SHA256) written alongside data file

### Long-Term (Planned)
- [ ] Migrate to database storage for production workloads
- [ ] Implement write-ahead log (WAL) pattern for file storage
- [ ] Add automated backup schedule with retention policy

## Lessons Learned
1. **Atomic writes are necessary but not sufficient** — the crash window between serialization and rename must be handled
2. **Health checks caught the issue quickly** — readiness endpoint correctly detected the storage problem
3. **Manual backups saved the day** — automated backups should be the default
4. **Import size limits are needed** — unbounded imports can cause resource exhaustion

## Metrics
- **Time to detect**: 5 minutes (health check monitoring)
- **Time to resolve**: 35 minutes
- **Data loss**: ~15 minutes of writes (between last backup and crash)
- **Users affected**: 1 organisation (3 users)
