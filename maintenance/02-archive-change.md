# Maintenance Scenario: Change Request

## Scenario

The product owner requests: "Add a `category` field to todos so users can group them by project area (e.g., Backend, Frontend, DevOps)."

## 1. Requirements Gathering

**Questions asked:**
- Is category free-text or a fixed set? → **Free-text string, max 50 chars**
- Is it required? → **Optional, defaults to null**
- Should it be filterable? → **Yes, add a `category` query parameter to the list endpoint**
- Should it appear in import/export? → **Yes**

## 2. Impact Analysis

### Files to Modify

| Layer | File | Change |
|-------|------|--------|
| Core | `Entities/Todo.cs` | Add `Category` property |
| Core | `ImportExportEntities/TodoExportModel.cs` | Add `Category` property |
| Core | `Entities/TodoFilter.cs` | Add `Category` filter |
| Core | `Entities/File-storage/TodoData.cs` | Add `Category` field |
| Application | `Dto/TodoDto.cs` | Add `Category` to Create, Update, Response DTOs |
| Application | `Extensions/MappingExtensions.cs` | Map `Category` in `ToResponse()` |
| Application | `Validators/TodoValidators.cs` | Add max-length rule |
| Application | `Services/ImportExportService.cs` | Map `Category` in import/export |
| Application | `Services/MigrationService.cs` | Add v2→v3 migration (default null) |
| Infrastructure | `Storage/InMemoryStorage.cs` | Add `Category` to filter logic |
| Infrastructure | `Storage/FileStorage.cs` | Add `Category` to filter logic |
| Api | `Controller/TodoController.cs` | Pass `Category` filter to query |
| Api | `Extensions/TodoExamples.cs` | Update Swagger examples |
| Frontend | `services/types.ts` | Add `category` to Todo type |
| Frontend | `App.tsx` | Add category input to create/edit forms, add filter |
| Tests | `Integration/TodoCrudTests.cs` | Test category create, update, filter |

### Risk Assessment

- **Low risk** — additive change, no existing fields modified
- **Migration needed** — FileStorage schema v2→v3 (add Category to existing todos as null)
- **Backward compatible** — existing imports without Category field will work (defaults to null)

## 3. Implementation Plan

### Step 1: Core Entity

```csharp
// Todo.cs
public string? Category { get; set; }

// TodoFilter.cs
public string? Category { get; set; }

// TodoExportModel.cs
public string? Category { get; set; }

// TodoData.cs (FileStorage)
public string? Category { get; set; }
```

### Step 2: DTOs

```csharp
// CreateTodoRequest
public string? Category { get; set; }

// UpdateTodoRequest
public string? Category { get; set; }

// TodoResponse
public string? Category { get; set; }
```

### Step 3: Validation

```csharp
// CreateTodoRequestValidator
RuleFor(x => x.Category)
    .MaximumLength(50).When(x => x.Category != null)
    .Matches(@"^[a-zA-Z0-9\s\-_]+$").When(x => x.Category != null);
```

### Step 4: Mapping

```csharp
// MappingExtensions.ToResponse()
Category = todo.Category
```

### Step 5: Storage Filter

```csharp
// In filter logic
if (!string.IsNullOrEmpty(filter.Category))
    todos = todos.Where(t => t.Category == filter.Category);
```

### Step 6: Controller

```csharp
// TodoController.Get — add query parameter
public async Task<IActionResult> GetTodos([FromQuery] TodoQuery query)
// TodoQuery already binds to TodoFilter

// Create action
todo.Category = request.Category;

// Update action
if (request.Category != null) todo.Category = request.Category;
```

### Step 7: Migration

```csharp
// MigrationService — add v2→v3
if (schema.SchemaVersion < 3)
{
    foreach (var todo in schema.Todos.Values)
    {
        todo.Category ??= null; // Already null by default
    }
    schema.SchemaVersion = 3;
}
```

### Step 8: Import/Export

```csharp
// ExportTodosAsync
Category = t.Category

// ImportSingleTodo
todo.Category = model.Category

// CSV header: add Category column
// CSV row: add category value
```

### Step 9: Frontend

```typescript
// types.ts
category?: string;

// App.tsx — create form
<input name="category" placeholder="Category (optional)" ... />

// App.tsx — edit form
<input name="category" value={editCategory} ... />

// App.tsx — filter
<select for category filter>
```

### Step 10: Tests

```csharp
[Fact]
public async Task CreateTodo_WithCategory_ShouldIncludeCategory()
{
    var response = await client.PostAsJsonAsync("/api/v1/todo", new
    {
        title = "Categorised Task",
        category = "Backend"
    });
    var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
    todo!.Category.Should().Be("Backend");
}

[Fact]
public async Task FilterByCategory_ShouldReturnMatching()
{
    // Create todos with different categories, filter, verify
}
```

## 4. Verification

```bash
# Build
dotnet build TaskHub.sln

# Test
dotnet test TaskHub.sln

# Manual verification
# 1. Create todo with category via Swagger
# 2. List todos, verify category returned
# 3. Filter by category, verify only matching returned
# 4. Export, verify category in output
# 5. Import with category, verify persisted
```

## 5. Document

### CHANGELOG entry

```markdown
### Added
- `category` field on todos for project-area grouping (optional, max 50 chars)
- Category filter on todo list endpoint (`?category=Backend`)
- Category in JSON and CSV import/export
- FileStorage schema migration v2 → v3
```

### Commit

```bash
git add -A
git commit -m "feat: add optional category field to todos

Users can now set a category (max 50 chars) on todos for
project-area grouping. Includes filtering, import/export,
and FileStorage schema migration v2→v3."
```

## 6. Rollback Plan

If issues arise:
1. The `Category` field is nullable — existing data is unaffected
2. Frontend gracefully handles `category: null`
3. FileStorage migration is forward-only; rollback would need schema version check bypass
4. Worst case: revert the commit; null categories remain harmless in storage file
