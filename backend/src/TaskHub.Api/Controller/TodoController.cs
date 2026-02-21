using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstraction;
using Task_hub.Application.Authorization;
using TaskHub.Api.Dto;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;

namespace TaskHub.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly IStorage _storage;
        private readonly IOrganisationContext _organisationContext;
        private readonly ILogger<TodoController> _logger;

        public TodoController(
            IStorage storage,
            IOrganisationContext organisationContext,
            ILogger<TodoController> logger)
        {
            _storage = storage;
            _organisationContext = organisationContext;
            _logger = logger;
        }

        [HttpGet]
        [RequireOrganisation]
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

            return Ok(todos.Select(MapToResponse));
        }

        [HttpGet("{id}")]
        [RequireOrganisation]
        public async Task<ActionResult<TodoResponse>> GetTodo(Guid id)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            // Add ETag for optimistic concurrency
            Response.Headers.ETag = $"\"{todo.Version}\"";

            return Ok(MapToResponse(todo));
        }

        [HttpPost]
        [RequireOrganisation]
        public async Task<ActionResult<TodoResponse>> CreateTodo(CreateTodoRequest request)
        {
            var userId = GetCurrentUserId();
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

            // Audit log
            await Audit("TodoCreated", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' created", userId.Value, orgId);

            return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, MapToResponse(todo));
        }

        [HttpPut("{id}")]
        [RequireOrganisation]
        public async Task<IActionResult> UpdateTodo(Guid id, UpdateTodoRequest request)
        {
            var userId = GetCurrentUserId();
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
                return StatusCode(412, new { error = "Todo was modified by another user" });
            }

            // Update fields
            todo.Title = request.Title ?? todo.Title;
            todo.Description = request.Description ?? todo.Description;
            todo.Priority = request.Priority ?? todo.Priority;
            todo.Tags = request.Tags ?? todo.Tags;
            todo.DueDate = request.DueDate ?? todo.DueDate;
            todo.UpdatedAt = DateTime.UtcNow;

            await _storage.UpdateTodoAsync(todo);

            // Audit log
            await Audit("TodoUpdated", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' updated", userId.Value, orgId);

            return Ok(MapToResponse(todo));
        }

        [HttpPatch("{id}/toggle")]
        [RequireOrganisation]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            todo.Status = todo.Status == TodoStatus.Open ? TodoStatus.Done : TodoStatus.Open;
            todo.UpdatedAt = DateTime.UtcNow;
            await _storage.UpdateTodoAsync(todo);

            // Audit log
            await Audit("TodoToggled", "Todo", todo.Id.ToString(),
                $"Todo status changed to {todo.Status}", userId.Value, orgId);

            return Ok(MapToResponse(todo));
        }

        [HttpDelete("{id}/soft")]
        [RequireOrganisation]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            todo.DeletedAt = DateTime.UtcNow;
            await _storage.UpdateTodoAsync(todo);

            // Audit log
            await Audit("TodoSoftDeleted", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' soft deleted", userId.Value, orgId);

            return Ok(new { message = "Todo soft deleted" });
        }

        [HttpPost("{id}/restore")]
        [RequireOrganisation]
        public async Task<IActionResult> Restore(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            todo.DeletedAt = null;
            await _storage.UpdateTodoAsync(todo);

            // Audit log
            await Audit("TodoRestored", "Todo", todo.Id.ToString(),
                $"Todo '{todo.Title}' restored", userId.Value, orgId);

            return Ok(MapToResponse(todo));
        }

        [HttpDelete("{id}")]
        [RequireOrganisation(RequireAdmin = true)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var todo = await _storage.GetTodoByIdAsync(id, orgId);

            if (todo == null)
                return NotFound();

            await _storage.DeleteTodoAsync(id, orgId);

            // Audit log
            await Audit("TodoHardDeleted", "Todo", id.ToString(),
                $"Todo permanently deleted", userId.Value, orgId);

            return Ok(new { message = "Todo permanently deleted" });
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;
            return null;
        }

        private async Task Audit(string action, string entityType, string entityId, string details, Guid actorId, Guid orgId)
        {
            var auditLog = new AuditLog
            {
                ActorUserId = actorId,
                OrganisationId = orgId,
                ActionType = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CorrelationId = HttpContext.TraceIdentifier
            };
            await _storage.AddAuditLogAsync(auditLog);
        }

        private TodoResponse MapToResponse(Todo todo)
        {
            return new TodoResponse
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                Status = todo.Status.ToString(),
                Priority = todo.Priority.ToString(),
                Tags = todo.Tags,
                DueDate = todo.DueDate,
                CreatedAt = todo.CreatedAt,
                UpdatedAt = todo.UpdatedAt,
                DeletedAt = todo.DeletedAt,
                Version = todo.Version
            };
        }
    }
}
