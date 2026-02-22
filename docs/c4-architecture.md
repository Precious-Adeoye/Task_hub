# C4 Architecture — TaskHub

## Level 1: System Context

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│                    TaskHub System                    │
│                                                     │
│  Secure multi-tenant todo management platform       │
│                                                     │
└──────────────────────┬──────────────────────────────┘
                       │
            ┌──────────┴──────────┐
            │                     │
      ┌─────▼─────┐        ┌─────▼─────┐
      │   User     │        │  Browser  │
      │ (Person)   │───────▶│  (React)  │
      │            │        │           │
      └────────────┘        └───────────┘
```

**Actors:**
- **User** — Team member or administrator who manages todos and organisations
- **Browser** — React SPA that communicates with the backend API

**External Systems:** None (self-contained application)

---

## Level 2: Container Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ TaskHub System                                                  │
│                                                                 │
│  ┌──────────────┐    HTTPS/JSON     ┌─────────────────────┐    │
│  │              │──────────────────▶│                     │    │
│  │  React SPA   │                   │  ASP.NET Core API   │    │
│  │  (Frontend)  │◀──────────────────│  (.NET 9)           │    │
│  │              │    JSON + Cookies  │                     │    │
│  │  Port 3000   │                   │  Port 5000          │    │
│  └──────────────┘                   └──────────┬──────────┘    │
│                                                │               │
│                                     ┌──────────┴──────────┐    │
│                                     │                     │    │
│                              ┌──────▼──────┐  ┌───────────▼┐  │
│                              │  InMemory   │  │   File     │  │
│                              │  Storage    │  │   Storage  │  │
│                              │ (default)   │  │  (JSON)    │  │
│                              └─────────────┘  └────────────┘  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

**Containers:**
- **React SPA** — Single-page application served on port 3000; handles auth, org management, todo UI, admin panel
- **ASP.NET Core API** — RESTful API on port 5000; 25 endpoints across 5 controllers
- **InMemory Storage** — ConcurrentDictionary-based volatile storage (dev/test)
- **File Storage** — JSON file persistence with atomic writes and schema migration

---

## Level 3: Component Diagram (API)

```
┌───────────────────────────────────────────────────────────────────────────┐
│ ASP.NET Core API                                                         │
│                                                                          │
│  ┌─── Middleware Pipeline ──────────────────────────────────────────┐    │
│  │                                                                  │    │
│  │  ProblemDetails → ETag → CorrelationId → CORS → RateLimit      │    │
│  │  → Authentication → Authorization                                │    │
│  │                                                                  │    │
│  └──────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  ┌─── Controllers (API Layer) ─────────────────────────────────────┐    │
│  │                                                                  │    │
│  │  AuthController        POST register, login, logout; GET me     │    │
│  │  TodoController        CRUD + toggle + soft/hard delete         │    │
│  │  OrganisationsController  Org CRUD + member management          │    │
│  │  AuditController       GET logs, GET summary                    │    │
│  │  ImportExportController  GET export; POST import; GET template  │    │
│  │                                                                  │    │
│  └──────────────────────────┬───────────────────────────────────────┘    │
│                              │                                           │
│  ┌─── Application Layer ────▼──────────────────────────────────────┐    │
│  │                                                                  │    │
│  │  Services:                                                       │    │
│  │    AuthService          Registration, login, password hashing   │    │
│  │    AuditService         Audit log creation                      │    │
│  │    ImportExportService  JSON/CSV import and export              │    │
│  │    OrganisationContext  Tenant resolution from headers          │    │
│  │    MigrationService     FileStorage schema migration            │    │
│  │                                                                  │    │
│  │  Abstractions:                                                   │    │
│  │    IStorage, IAuthService, IAuditService, IImportExportService  │    │
│  │    IOrganisationContext, IMigrationService                      │    │
│  │                                                                  │    │
│  │  Validators:                                                     │    │
│  │    RegisterRequest, LoginRequest, CreateTodoRequest              │    │
│  │    UpdateTodoRequest, CreateOrganisationRequest, AddMemberRequest│    │
│  │                                                                  │    │
│  │  Extensions:                                                     │    │
│  │    ClaimsPrincipalExtensions, MappingExtensions                  │    │
│  │                                                                  │    │
│  │  Authorization:                                                  │    │
│  │    RequireOrganisationAttribute, OrganisationAuthorizationHandler│    │
│  │                                                                  │    │
│  └──────────────────────────┬───────────────────────────────────────┘    │
│                              │                                           │
│  ┌─── Core Layer ───────────▼──────────────────────────────────────┐    │
│  │                                                                  │    │
│  │  Entities: User, Todo, Organisation, Membership, AuditLog       │    │
│  │  Enums: TodoStatus, Priority, Role                              │    │
│  │  Import: TodoExportModel, ImportResult, ImportOptions            │    │
│  │  File Storage: FileStorageSchema, UserData, TodoData, etc.      │    │
│  │                                                                  │    │
│  └──────────────────────────┬───────────────────────────────────────┘    │
│                              │                                           │
│  ┌─── Infrastructure Layer ─▼──────────────────────────────────────┐    │
│  │                                                                  │    │
│  │  InMemoryStorage        ConcurrentDictionary-based              │    │
│  │  FileStorage            JSON file with SemaphoreSlim locking    │    │
│  │                                                                  │    │
│  └──────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Level 4: Code Diagram (Todo Update Flow)

```
Client                    TodoController          IStorage          AuditService
  │                            │                     │                   │
  │  PUT /api/v1/todo/{id}     │                     │                   │
  │  If-Match: "abc123"        │                     │                   │
  │  { title: "New Title" }    │                     │                   │
  │───────────────────────────▶│                     │                   │
  │                            │                     │                   │
  │                            │  GetTodoByIdAsync   │                   │
  │                            │────────────────────▶│                   │
  │                            │◀────────────────────│                   │
  │                            │  todo (version="abc123")                │
  │                            │                     │                   │
  │                            │  Compare If-Match   │                   │
  │                            │  with todo.Version  │                   │
  │                            │                     │                   │
  │                            │  ✓ Match            │                   │
  │                            │                     │                   │
  │                            │  Apply updates      │                   │
  │                            │  todo.Version = new │                   │
  │                            │                     │                   │
  │                            │  UpdateTodoAsync    │                   │
  │                            │────────────────────▶│                   │
  │                            │                     │                   │
  │                            │  AuditAsync         │                   │
  │                            │─────────────────────────────────────────▶
  │                            │                     │                   │
  │  200 OK                    │                     │                   │
  │  ETag: "def456"            │                     │                   │
  │  { id, title, version }    │                     │                   │
  │◀───────────────────────────│                     │                   │
```

**On version mismatch:**
```
  │                            │  Compare If-Match   │
  │                            │  with todo.Version  │
  │                            │                     │
  │                            │  ✗ Mismatch         │
  │                            │                     │
  │  412 Precondition Failed   │                     │
  │  ProblemDetails            │                     │
  │◀───────────────────────────│                     │
```

---

## Dependency Direction

```
  Api ──────▶ Application ──────▶ Core ◀────── Infrastructure
  (outer)       (middle)        (inner)         (outer)
```

- **Core** has zero dependencies — pure domain entities and enums
- **Application** depends only on Core — services, DTOs, validators, abstractions
- **Infrastructure** depends on Core and Application — implements `IStorage`
- **Api** depends on Application and Infrastructure — wires everything together

This follows the **Dependency Inversion Principle**: high-level modules (Application) define abstractions (`IStorage`), and low-level modules (Infrastructure) provide implementations.
