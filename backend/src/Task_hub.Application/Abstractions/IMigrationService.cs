using TaskHub.Core.Entities.File_storage;

namespace Task_hub.Application.Abstractions
{
    public interface IMigrationService
    {
        Task<FileStorageSchema> MigrateIfNeededAsync(FileStorageSchema schema);
    }
}
