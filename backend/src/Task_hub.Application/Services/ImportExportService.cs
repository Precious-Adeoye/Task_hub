using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Task_hub.Application.Abstractions;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;
using TaskHub.Core.ImportExportEntities;

namespace Task_hub.Application.Services
{
    public class ImportExportService : IImportExportService
    {
        private readonly IStorage _storage;
        private readonly ILogger<ImportExportService> _logger;

        public ImportExportService(IStorage storage, ILogger<ImportExportService> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public async Task<string> ExportTodosAsync(Guid organisationId, string format = "json")
        {
            var todos = await _storage.GetTodosAsync(organisationId, new TodoFilter { IncludeDeleted = false });

            var exportModels = todos.Select(t => new TodoExportModel
            {
                ClientProvidedId = t.Id.ToString(),
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Tags = t.Tags,
                DueDate = t.DueDate
            });

            if (format.ToLower() == "csv")
            {
                return ExportToCsv(exportModels);
            }
            else
            {
                return JsonSerializer.Serialize(exportModels, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        public async Task<ImportResult> ImportTodosAsync(Guid organisationId, string content, string format, ImportOptions? options = null)
        {
            options ??= new ImportOptions();
            var result = new ImportResult();

            try
            {
                List<TodoExportModel> importModels;

                if (format.ToLower() == "csv")
                {
                    importModels = ImportFromCsv(content);
                }
                else
                {
                    importModels = JsonSerializer.Deserialize<List<TodoExportModel>>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })
                        ?? new List<TodoExportModel>();
                }

                int rowNumber = 1;
                var existingTodos = options.Idempotent
                    ? (await _storage.GetTodosAsync(organisationId, new TodoFilter { IncludeDeleted = true }))
                        .ToDictionary(t => t.Id.ToString())
                    : new Dictionary<string, Todo>();

                foreach (var model in importModels)
                {
                    try
                    {
                        await ImportSingleTodo(organisationId, model, rowNumber, existingTodos, options, result);
                        result.AcceptedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.RejectedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ClientProvidedId = model.ClientProvidedId,
                            ErrorMessage = ex.Message
                        });

                        _logger.LogWarning(ex, "Failed to import todo at row {RowNumber}", rowNumber);
                    }

                    rowNumber++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed completely");
                throw new InvalidOperationException("Import failed: " + ex.Message, ex);
            }

            return result;
        }

        private async Task ImportSingleTodo(
       Guid organisationId,
       TodoExportModel model,
       int rowNumber,
       Dictionary<string, Todo> existingTodos,
       ImportOptions options,
       ImportResult result)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new InvalidOperationException("Title is required");
            }

            if (model.Title.Length > 200)
            {
                throw new InvalidOperationException("Title must not exceed 200 characters");
            }

            if (model.Description?.Length > 1000)
            {
                throw new InvalidOperationException("Description must not exceed 1000 characters");
            }

            // Validate status
            if (!Enum.TryParse<TodoStatus>(model.Status, true, out var status))
            {
                throw new InvalidOperationException($"Invalid status: {model.Status}. Must be Open or Done");
            }

            // Validate priority
            if (!Enum.TryParse<Priority>(model.Priority, true, out var priority))
            {
                throw new InvalidOperationException($"Invalid priority: {model.Priority}. Must be Low, Medium, or High");
            }

            // Validate tags
            foreach (var tag in model.Tags)
            {
                if (tag.Length > 50)
                {
                    throw new InvalidOperationException($"Tag '{tag}' exceeds 50 characters");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-Z0-9\-_]+$"))
                {
                    throw new InvalidOperationException($"Tag '{tag}' contains invalid characters. Use letters, numbers, hyphens, and underscores only");
                }
            }

            // Check for existing todo (idempotency)
            if (options.Idempotent && !string.IsNullOrEmpty(model.ClientProvidedId))
            {
                if (existingTodos.TryGetValue(model.ClientProvidedId, out var existingTodo))
                {
                    if (!options.OverwriteExisting)
                    {
                        // Skip silently for idempotency
                        return;
                    }

                    // Update existing todo
                    existingTodo.Title = model.Title;
                    existingTodo.Description = model.Description;
                    existingTodo.Status = status;
                    existingTodo.Priority = priority;
                    existingTodo.Tags = model.Tags;
                    existingTodo.DueDate = model.DueDate;
                    existingTodo.UpdatedAt = DateTime.UtcNow;

                    await _storage.UpdateTodoAsync(existingTodo);
                    return;
                }
            }

            // Create new todo
            var todo = new Todo
            {
                OrganisationId = organisationId,
                Title = model.Title,
                Description = model.Description,
                Status = status,
                Priority = priority,
                Tags = model.Tags,
                DueDate = model.DueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _storage.AddTodoAsync(todo);
        }

        private string ExportToCsv(IEnumerable<TodoExportModel> todos)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("ClientProvidedId,Title,Description,Status,Priority,Tags,DueDate");

            // Rows
            foreach (var todo in todos)
            {
                var tags = string.Join(";", todo.Tags);
                var dueDate = todo.DueDate?.ToString("yyyy-MM-dd") ?? "";

                sb.AppendLine($"\"{todo.ClientProvidedId}\",\"{EscapeCsv(todo.Title)}\",\"{EscapeCsv(todo.Description)}\",{todo.Status},{todo.Priority},\"{tags}\",{dueDate}");
            }

            return sb.ToString();
        }

        private List<TodoExportModel> ImportFromCsv(string csvContent)
        {
            var result = new List<TodoExportModel>();
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2) return result;

            var headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Length < 7) continue;

                var model = new TodoExportModel
                {
                    ClientProvidedId = UnescapeCsv(values[0]),
                    Title = UnescapeCsv(values[1]),
                    Description = UnescapeCsv(values[2]),
                    Status = values[3],
                    Priority = values[4],
                    Tags = UnescapeCsv(values[5]).Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    DueDate = DateTime.TryParse(values[6], out var dueDate) ? dueDate : null
                };

                result.Add(model);
            }

            return result;
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        private string UnescapeCsv(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
                value = value.Replace("\"\"", "\"");
            }
            return value;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentValue = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            result.Add(currentValue.ToString());
            return result.ToArray();
        }
    }
}
