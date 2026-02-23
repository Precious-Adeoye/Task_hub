# ADR-004: Schema Versioning and Migration Approach

## Status
Accepted

## Context
File-based storage persists data as JSON. As the application evolves, the data schema changes. We need a way to migrate existing data files to new schema versions without data loss.

## Decision
Embed a `schemaVersion` field in the file storage JSON. On startup, check the version and apply sequential migrations (v0->v1->v2) automatically.

## Options Considered

### Option A: No Versioning (Destructive)
- Simply overwrite — data loss on schema changes
- Unacceptable for any production use

### Option B: Embedded Schema Version with Auto-Migration (Chosen)
- Store `schemaVersion` integer in the JSON root
- On load, compare against current version
- Apply migration functions sequentially
- Atomic write after migration completes

### Option C: Separate Migration Scripts
- External migration tool/scripts
- More complex setup, overkill for file-based JSON storage

## Consequences
- Migrations: v0->v1 adds Version field to todos, v1->v2 adds Description field
- Each migration is a code function that transforms the data structure
- Migration is idempotent — running on an already-migrated file is a no-op
- Migration test proves v1->v2 transformation works correctly

## Follow-ups
- Add migration rollback capability
- Consider backup-before-migrate strategy
- Document migration authoring guide for future developers
