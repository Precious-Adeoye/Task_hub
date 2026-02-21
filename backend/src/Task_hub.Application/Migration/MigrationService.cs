using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Core.Entities.File_storage;

namespace Task_hub.Application.Migration
{
    public interface IMigrationService
    {
        Task<FileStorageSchema> MigrateIfNeededAsync(FileStorageSchema schema);
    }
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

            // Migrate from v0 (no version) to v1
            if (currentVersion == 0)
            {
                _logger.LogInformation("Migrating schema from v0 to v1");
                schema = MigrateV0ToV1(schema);
                currentVersion = 1;
            }

            // Migrate from v1 to v2 (example future migration)
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
            // V0 -> V1: Add Version field to todos if missing
            foreach (var todo in schema.Todos.Values)
            {
                if (string.IsNullOrEmpty(todo.Version))
                {
                    todo.Version = Guid.NewGuid().ToString();
                }
            }

            // Add any other v1 migration logic here
            return schema;
        }

        private FileStorageSchema MigrateV1ToV2(FileStorageSchema schema)
        {
            // Example v2 migration: Add new field or transform data
            // For demonstration, we'll add a default description to todos that don't have one
            foreach (var todo in schema.Todos.Values)
            {
                if (string.IsNullOrEmpty(todo.Description))
                {
                    todo.Description = ""; // Set empty string instead of null
                }
            }

            return schema;
        }
    }
}
