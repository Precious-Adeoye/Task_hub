# Maintenance Scenario: Bug Fix

## Scenario

A user reports that importing a JSON file with camelCase property names results in 0 accepted items, even though the file is valid.

## 1. Reproduce

```bash
# Create a test file with camelCase
cat > test-import.json << 'EOF'
[
  {
    "clientProvidedId": "bug-1",
    "title": "Test Import",
    "status": "Open",
    "priority": "High"
  }
]
EOF

# Import via file upload
curl -b cookies.txt \
  -F "file=@test-import.json" \
  "http://localhost:5000/api/v1/importexport/import?format=json"
```

**Expected:** `{ "acceptedCount": 1, "rejectedCount": 0 }`
**Actual:** `{ "acceptedCount": 0, "rejectedCount": 1, "errors": [{ "errorMessage": "Title is required" }] }`

## 2. Diagnose

### Check Logs

```bash
grep "Failed to import" logs/taskhub-$(date +%Y%m%d).txt
```

Log shows: `Failed to import todo at row 1 — Title is required`

### Root Cause Analysis

1. Open `ImportExportService.cs` — the import method
2. Find the JSON deserialization call:
   ```csharp
   JsonSerializer.Deserialize<List<TodoExportModel>>(content)
   ```
3. `System.Text.Json` is **case-sensitive by default**
4. The `TodoExportModel` has PascalCase properties (`Title`, `Status`)
5. The user's JSON has camelCase (`title`, `status`)
6. All properties deserialize as `null` / default → validation fails

### Verify with Unit Test

Write a failing test first:
```csharp
[Fact]
public async Task ImportJsonFile_CamelCase_ShouldImportTodos()
{
    var client = await CreateAuthenticatedClient();
    var json = "[{\"clientProvidedId\":\"cc-1\",\"title\":\"CamelCase\",\"status\":\"Open\",\"priority\":\"High\"}]";
    var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
    content.Add(fileContent, "file", "todos.json");

    var response = await client.PostAsync("/api/v1/importexport/import?format=json", content);
    var result = await response.Content.ReadFromJsonAsync<ImportResult>();

    result!.AcceptedCount.Should().Be(1);  // FAILS before fix
}
```

## 3. Fix

**File:** `src/Task_hub.Application/Services/ImportExportService.cs`

**Change:**
```csharp
// Before (case-sensitive)
importModels = JsonSerializer.Deserialize<List<TodoExportModel>>(content)
    ?? new List<TodoExportModel>();

// After (case-insensitive)
importModels = JsonSerializer.Deserialize<List<TodoExportModel>>(content,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? new List<TodoExportModel>();
```

## 4. Verify

```bash
# Run the specific test
dotnet test --filter "ImportJsonFile_CamelCase"

# Run all tests to check for regressions
dotnet test TaskHub.sln
```

**Result:** 31/31 tests pass.

## 5. Document

### CHANGELOG entry

```markdown
### Fixed
- Import via file upload failing on camelCase JSON input
```

### Commit

```bash
git add src/Task_hub.Application/Services/ImportExportService.cs
git add tests/TaskHub.Tests/Integration/ImportExportTests.cs
git commit -m "fix: import now accepts camelCase JSON input

Added PropertyNameCaseInsensitive = true to JSON deserialization
in ImportExportService. Added regression test."
```

## 6. Lessons Learned

- `System.Text.Json` defaults differ from `Newtonsoft.Json` (which is case-insensitive by default)
- All JSON deserialization points that accept external input should use `PropertyNameCaseInsensitive = true`
- Import tests should cover both PascalCase and camelCase inputs
