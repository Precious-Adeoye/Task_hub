# ADR-003: Storage Abstraction and File Storage Strategy

## Status
Accepted

## Context
TaskHub must support both in-memory and file-based storage, switchable via configuration. The storage layer must be abstracted so the application logic is independent of the storage mechanism.

## Decision
Define an `IStorage` interface in the Application layer. Implement `InMemoryStorage` using `ConcurrentDictionary` and `FileStorage` using JSON files with atomic writes. Switch via `StorageProvider` config setting.

## Options Considered

### Option A: Repository Pattern per Entity
- Separate interface per entity (ITodoRepository, IUserRepository)
- More granular but more interfaces to maintain

### Option B: Unified Storage Interface (Chosen)
- Single `IStorage` interface with generic methods for all entities
- Simpler to implement and swap storage providers
- DI registration switches the entire storage layer at once

### Option C: Entity Framework with In-Memory and SQLite providers
- Heavy ORM dependency for a simple storage requirement
- EF In-Memory provider has known differences from real databases

## Consequences
- File storage uses temp-file + rename pattern for atomic writes
- File storage uses `SemaphoreSlim` for concurrency safety
- 5-second cache prevents excessive file I/O on reads
- One JSON file per data store with all entities serialized together
- Both providers must implement identical concurrency (version/ETag) behavior

## Follow-ups
- Consider per-org file storage for better isolation at scale
- Add database provider (SQLite/PostgreSQL) for production use
- Evaluate file locking strategy for multi-process scenarios
