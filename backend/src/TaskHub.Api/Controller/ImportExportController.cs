using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Authorization;
using Task_hub.Application.Extensions;
using TaskHub.Core.Entities;
using TaskHub.Core.ImportExportEntities;

namespace TaskHub.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ImportExportController : ControllerBase
    {
        private readonly IImportExportService _importExportService;
        private readonly IAuditService _auditService;
        private readonly IOrganisationContext _organisationContext;
        private readonly ILogger<ImportExportController> _logger;

        public ImportExportController(
            IImportExportService importExportService,
            IAuditService auditService,
            IOrganisationContext organisationContext,
            ILogger<ImportExportController> logger)
        {
            _importExportService = importExportService;
            _auditService = auditService;
            _organisationContext = organisationContext;
            _logger = logger;
        }

        [HttpGet("export")]
        [RequireOrganisation]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Export([FromQuery] string format = "json")
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var userId = User.GetUserId()!.Value;

            var content = await _importExportService.ExportTodosAsync(orgId, format);

            await _auditService.AuditAsync("TodosExported", "Export", orgId.ToString(),
                $"Exported todos as {format}", userId, orgId);

            var contentType = format.ToLower() == "csv"
                ? "text/csv"
                : "application/json";

            var fileName = $"todos-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format}";

            return File(Encoding.UTF8.GetBytes(content), contentType, fileName);
        }

        [HttpPost("import")]
        [RequireOrganisation]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
        [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImportResult>> Import(
             IFormFile file,
           [FromQuery] string format = "json",
           [FromQuery] bool idempotent = true,
           [FromQuery] bool overwrite = false)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var userId = User.GetUserId()!.Value;

            if (file == null || file.Length == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "No file uploaded",
                    Detail = "Please provide a file to import",
                    Status = 400
                });
            }

            // Check file extension matches format
            var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            if (extension != format && format != "auto")
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Format mismatch",
                    Detail = $"File extension '{extension}' does not match requested format '{format}'",
                    Status = 400
                });
            }

            // Read file content
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            var options = new ImportOptions
            {
                Idempotent = idempotent,
                OverwriteExisting = overwrite
            };

            var result = await _importExportService.ImportTodosAsync(orgId, content, format, options);

            await _auditService.AuditAsync("TodosImported", "Import", orgId.ToString(),
                $"Imported {result.AcceptedCount} todos, {result.RejectedCount} rejected", userId, orgId);

            if (result.RejectedCount > 0)
            {
                return Ok(result); // Still OK but with errors
            }

            return Ok(result);
        }

        [HttpPost("import/json")]
        [RequireOrganisation]
        [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<ImportResult>> ImportJson([FromBody] List<TodoExportModel> todos)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var userId = User.GetUserId()!.Value;

            var json = JsonSerializer.Serialize(todos);
            var options = new ImportOptions { Idempotent = true };

            var result = await _importExportService.ImportTodosAsync(orgId, json, "json", options);

            await _auditService.AuditAsync("TodosImported", "Import", orgId.ToString(),
                $"Imported {result.AcceptedCount} todos via JSON", userId, orgId);

            return Ok(result);
        }


        [HttpGet("template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetTemplate([FromQuery] string format = "json")
        {
            var template = new List<TodoExportModel>
        {
            new TodoExportModel
            {
                ClientProvidedId = "example-1",
                Title = "Complete project documentation",
                Description = "Write comprehensive docs for the API",
                Status = "Open",
                Priority = "High",
                Tags = new List<string> { "documentation", "urgent" },
                DueDate = DateTime.UtcNow.AddDays(7)
            },
            new TodoExportModel
            {
                ClientProvidedId = "example-2",
                Title = "Review pull requests",
                Description = "Review and merge pending PRs",
                Status = "Open",
                Priority = "Medium",
                Tags = new List<string> { "development" },
                DueDate = DateTime.UtcNow.AddDays(2)
            }
        };

            if (format.ToLower() == "csv")
            {
                var csvContent = _importExportService.ExportTodosAsync(Guid.NewGuid(), "csv").Result;
                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", "import-template.csv");
            }
            else
            {
                var jsonContent = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                return File(Encoding.UTF8.GetBytes(jsonContent), "application/json", "import-template.json");
            }
        }
    }
}
