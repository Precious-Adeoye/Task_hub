using TaskHub.Core.ImportExportEntities;

namespace Task_hub.Application.Abstractions
{
    public interface IImportExportService
    {
        Task<string> ExportTodosAsync(Guid organisationId, string format = "json");
        Task<ImportResult> ImportTodosAsync(Guid organisationId, string content, string format, ImportOptions? options = null);
    }
}
