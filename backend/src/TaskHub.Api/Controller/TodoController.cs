using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Authorization;
using Task_hub.Application.Extensions;
using TaskHub.Api.Dto;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;

namespace TaskHub.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class TodoController : ControllerBase
    {
        private readonly IStorage _storage;
        private readonly IAuditService _auditService;
        private readonly IOrganisationContext _organisationContext;
        private readonly ILogger<TodoController> _logger;

        public TodoController(
            IStorage storage,
            IAuditService auditService,
            IOrganisationContext organisationContext,
            ILogger<TodoController> logger)
        {
            _storage = storage;
            _auditService = auditService;
            _organisationContext = organisationContext;
            _logger = logger;
        }

        [HttpGet]
        [RequireOrganisation]
        [ProducesResponseType(typeof(IEnumerable<TodoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos([FromQuery] TodoQuery query)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;

            var filter = new TodoFilter
            {
                Status = query.Status,
                Overdue = query.Overdue,
                Tag = query.Tag,
                IncludeDeleted = query.IncludeDeleted ?? false,
                Page = query.Page ?? 1,
                PageSize = query.PageSize ?? 20,
                SortBy = query.SortBy ?? "createdAt",
                SortDescending = query.SortDescending ?? true
            };

            var todos = await _storage.GetTodosAsync(orgId, filter);

            Response.Headers.Append("X-Total-Count", todos.Count().ToString());

            return Ok(todos.Select(t => t.ToResponse()));
        }

        [HttpGet("{id}")]
        [RequireOrganisation]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TodoResponse>> GetTodo(Guid id)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            // Add ETag for optimistic concurrency
            Response.Headers.ETag = $"\"{todo.Version}\"";

            return Ok(todo.ToResponse());
        }

        [HttpPost]
        [RequireOrganisation]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TodoResponse>> CreateTodo(CreateTodoRequest request)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;

            var todo = new Todo
            {
                OrganisationId = orgId,
                CreatedBy = userId.Value,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Tags = request.Tags ?? new List<string>(),
                DueDate = request.DueDate
            };

            await _storage.AddTodoAsync(todo);

            await _auditService.AuditAsync("TodoCreated", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' created", userId.Value, orgId);

            return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, todo.ToResponse());
        }

        [HttpPut("{id}")]
        [RequireOrganisation]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
        public async Task<IActionResult> UpdateTodo(Guid id, UpdateTodoRequest request)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            // Check ETag for optimistic concurrency
            var ifMatch = Request.Headers.IfMatch.ToString();
            if (!string.IsNullOrEmpty(ifMatch) && ifMatch.Trim('"') != todo.Version)
            {
                return StatusCode(412, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7232#section-4.2",
                    Title = "Precondition Failed",
                    Status = 412,
                    Detail = "Todo was modified by another user. Refresh and retry.",
                    Instance = Request.Path
                });
            }

            // Update fields
            todo.Title = request.Title ?? todo.Title;
            todo.Description = request.Description ?? todo.Description;
            todo.Priority = request.Priority ?? todo.Priority;
            todo.Tags = request.Tags ?? todo.Tags;
            todo.DueDate = request.DueDate ?? todo.DueDate;
            todo.UpdatedAt = DateTime.UtcNow;

            await _storage.UpdateTodoAsync(todo);

            await _auditService.AuditAsync("TodoUpdated", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' updated", userId.Value, orgId);

            return Ok(todo.ToResponse());
        }

        [HttpPatch("{id}/toggle")]
        [RequireOrganisation]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            todo.Status = todo.Status == TodoStatus.Open ? TodoStatus.Done : TodoStatus.Open;
            todo.UpdatedAt = DateTime.UtcNow;
            await _storage.UpdateTodoAsync(todo);

            await _auditService.AuditAsync("TodoToggled", "Todo", todo.Id.ToString(),
                $"Todo status changed to {todo.Status}", userId.Value, orgId);

            return Ok(todo.ToResponse());
        }

        [HttpDelete("{id}/soft")]
        [RequireOrganisation]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            var ifMatch = Request.Headers.IfMatch.ToString();
            if (!string.IsNullOrEmpty(ifMatch) && ifMatch.Trim('"') != todo.Version)
                return StatusCode(412, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7232#section-4.2",
                    Title = "Precondition Failed",
                    Status = 412,
                    Detail = "Todo was modified by another user. Refresh and retry.",
                    Instance = Request.Path
                });

            todo.DeletedAt = DateTime.UtcNow;
            await _storage.UpdateTodoAsync(todo);

            await _auditService.AuditAsync("TodoSoftDeleted", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' soft deleted", userId.Value, orgId);

            return Ok(new { message = "Todo soft deleted" });
        }

        [HttpPost("{id}/restore")]
        [RequireOrganisation]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
        public async Task<IActionResult> Restore(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            var ifMatch = Request.Headers.IfMatch.ToString();
            if (!string.IsNullOrEmpty(ifMatch) && ifMatch.Trim('"') != todo.Version)
                return StatusCode(412, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7232#section-4.2",
                    Title = "Precondition Failed",
                    Status = 412,
                    Detail = "Todo was modified by another user. Refresh and retry.",
                    Instance = Request.Path
                });

            todo.DeletedAt = null;
            await _storage.UpdateTodoAsync(todo);

            await _auditService.AuditAsync("TodoRestored", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' restored", userId.Value, orgId);

            return Ok(todo.ToResponse());
        }

        [HttpDelete("{id}")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            var ifMatch = Request.Headers.IfMatch.ToString();
            if (!string.IsNullOrEmpty(ifMatch) && ifMatch.Trim('"') != todo.Version)
                return StatusCode(412, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7232#section-4.2",
                    Title = "Precondition Failed",
                    Status = 412,
                    Detail = "Todo was modified by another user. Refresh and retry.",
                    Instance = Request.Path
                });

            await _storage.DeleteTodoAsync(id, orgId);

            await _auditService.AuditAsync("TodoHardDeleted", "Todo", id.ToString(),
                $"Todo permanently deleted", userId.Value, orgId);

            return Ok(new { message = "Todo permanently deleted" });
        }
    }
}
