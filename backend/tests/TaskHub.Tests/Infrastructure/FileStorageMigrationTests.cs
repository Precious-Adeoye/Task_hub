using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TaskHub.Core.Entities;
using TaskHub.Core.Entities.File_storage;
using TaskHub.Core.Enum;
using TaskHub.Infrastructure.Storage;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Services;
using Xunit;

namespace TaskHub.Tests.Infrastructure;

public class FileStorageMigrationTests : IDisposable
{
    private readonly string _testPath;
    private readonly FileStorage _storage;
    private readonly MigrationService _migrationService;

    public FileStorageMigrationTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"taskhub-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Path"] = _testPath
            })
            .Build();

        _migrationService = new MigrationService(new NullLogger<MigrationService>());
        _storage = new FileStorage(configuration, new NullLogger<FileStorage>(), _migrationService);
    }

    [Fact]
    public async Task MigrateV1ToV2_AddsEmptyDescriptionToTodos()
    {
        // Arrange - Create v1 schema
        var v1Schema = new FileStorageSchema
        {
            SchemaVersion = 1,
            LastModified = DateTime.UtcNow,
            Todos = new Dictionary<Guid, TodoData>
            {
                [Guid.NewGuid()] = new TodoData
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Todo",
                    Description = null, // v1 had null descriptions
                    Status = TodoStatus.Open,
                    Version = Guid.NewGuid().ToString()
                }
            }
        };

        // Save v1 schema
        var v1Path = Path.Combine(_testPath, "taskhub-data.json");
        var json = System.Text.Json.JsonSerializer.Serialize(v1Schema);
        await File.WriteAllTextAsync(v1Path, json);

        // Act - Read schema (triggers migration)
        var migratedSchema = await _migrationService.MigrateIfNeededAsync(v1Schema);

        // Assert
        Assert.Equal(2, migratedSchema.SchemaVersion);
        foreach (var todo in migratedSchema.Todos.Values)
        {
            Assert.NotNull(todo.Description);
            Assert.Equal("", todo.Description);
        }
    }

    [Fact]
    public async Task AtomicWrite_PreventsCorruption()
    {
        // Arrange
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Test Todo",
            OrganisationId = Guid.NewGuid()
        };

        // Act
        await _storage.AddTodoAsync(todo);

        // Write partial JSON to a .tmp file (simulating interrupted write)
        var storagePath = Path.Combine(_testPath, "taskhub-data.json");
        var tempPath = storagePath + ".tmp";
        var partialJson = "{ \"SchemaVersion\": 2, \"Todos\": {"; // Incomplete JSON
        await File.WriteAllTextAsync(tempPath, partialJson);

        // Assert - Original file should still be intact
        var recoveredTodo = await _storage.GetTodoByIdAsync(todo.Id, todo.OrganisationId);
        Assert.NotNull(recoveredTodo);
        Assert.Equal(todo.Title, recoveredTodo.Title);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}
