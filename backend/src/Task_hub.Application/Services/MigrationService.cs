using Microsoft.Extensions.Logging;
using Task_hub.Application.Abstractions;
using TaskHub.Core.Entities.File_storage;

namespace Task_hub.Application.Services
{
    public class MigrationService : IMigrationService
    {
        private readonly ILogger<MigrationService> _logger;

        public MigrationService(ILogger<MigrationService> logger)
        {
            _logger = logger;
        }

        public Task<FileStorageSchema> MigrateIfNeededAsync(FileStorageSchema schema)
        {
            var currentVersion = schema.SchemaVersion;

            if (currentVersion == 0)
            {
                _logger.LogInformation("Migrating schema from v0 to v1");
                schema = MigrateV0ToV1(schema);
                currentVersion = 1;
            }

            if (currentVersion == 1)
            {
                _logger.LogInformation("Migrating schema from v1 to v2");
                schema = MigrateV1ToV2(schema);
                currentVersion = 2;
            }

            schema.SchemaVersion = currentVersion;
            schema.LastModified = DateTime.UtcNow;

            return Task.FromResult(schema);
        }

        private FileStorageSchema MigrateV0ToV1(FileStorageSchema schema)
        {
            foreach (var todo in schema.Todos.Values)
            {
                if (string.IsNullOrEmpty(todo.Version))
                {
                    todo.Version = Guid.NewGuid().ToString();
                }
            }

            return schema;
        }

        private FileStorageSchema MigrateV1ToV2(FileStorageSchema schema)
        {
            foreach (var todo in schema.Todos.Values)
            {
                if (string.IsNullOrEmpty(todo.Description))
                {
                    todo.Description = "";
                }
            }

            return schema;
        }
    }
}
