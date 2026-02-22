using Swashbuckle.AspNetCore.Filters;
using TaskHub.Api.Dto;
using Task_hub.Application.Dto;
using TaskHub.Core.Enum;
using TaskHub.Core.ImportExportEntities;

namespace TaskHub.Api.Extensions;

// --- Auth Examples ---

public class RegisterRequestExample : IExamplesProvider<RegisterRequest>
{
    public RegisterRequest GetExamples() => new()
    {
        Username = "johndoe",
        Email = "john@example.com",
        Password = "SecureP@ss1"
    };
}

public class LoginRequestExample : IExamplesProvider<LoginRequest>
{
    public LoginRequest GetExamples() => new()
    {
        Username = "johndoe",
        Password = "SecureP@ss1"
    };
}

public class AuthResponseExample : IExamplesProvider<AuthResponse>
{
    public AuthResponse GetExamples() => new()
    {
        Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        Username = "johndoe",
        Email = "john@example.com"
    };
}

// --- Todo Examples ---

public class CreateTodoRequestExample : IExamplesProvider<CreateTodoRequest>
{
    public CreateTodoRequest GetExamples() => new()
    {
        Title = "Complete project documentation",
        Description = "Write comprehensive documentation for the TaskHub API",
        Priority = Priority.High,
        Tags = new List<string> { "documentation", "urgent" },
        DueDate = DateTime.UtcNow.AddDays(7)
    };
}

public class TodoResponseExample : IExamplesProvider<TodoResponse>
{
    public TodoResponse GetExamples() => new()
    {
        Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
        Title = "Complete project documentation",
        Description = "Write comprehensive documentation for the TaskHub API",
        Status = "Open",
        Priority = "High",
        Tags = new List<string> { "documentation", "urgent" },
        DueDate = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow,
        Version = "v1-abc123"
    };
}

// --- Organisation Examples ---

public class CreateOrganisationRequestExample : IExamplesProvider<CreateOrganisationRequest>
{
    public CreateOrganisationRequest GetExamples() => new()
    {
        Name = "Acme Corp"
    };
}

public class OrganisationResponseExample : IExamplesProvider<OrganisationResponse>
{
    public OrganisationResponse GetExamples() => new()
    {
        Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
        Name = "Acme Corp",
        CreatedAt = DateTime.UtcNow.AddDays(-30),
        CreatedBy = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890")
    };
}

public class AddMemberRequestExample : IExamplesProvider<AddMemberRequest>
{
    public AddMemberRequest GetExamples() => new()
    {
        Email = "jane@example.com",
        Role = "Member"
    };
}

public class MemberResponseExample : IExamplesProvider<MemberResponse>
{
    public MemberResponse GetExamples() => new()
    {
        UserId = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"),
        Username = "janedoe",
        Email = "jane@example.com",
        Role = "Member",
        JoinedAt = DateTime.UtcNow.AddDays(-7)
    };
}

// --- Audit Examples ---

public class AuditEntryResponseExample : IExamplesProvider<AuditEntryResponse>
{
    public AuditEntryResponse GetExamples() => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow.AddMinutes(-5),
        ActorUserId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        ActionType = "TodoCreated",
        EntityType = "Todo",
        EntityId = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        Details = "Todo 'Complete project documentation' created",
        CorrelationId = "corr-abc123"
    };
}

public class AuditLogResponseExample : IExamplesProvider<AuditLogResponse>
{
    public AuditLogResponse GetExamples() => new()
    {
        Logs = new List<AuditEntryResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                ActorUserId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                ActionType = "TodoCreated",
                EntityType = "Todo",
                EntityId = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
                Details = "Todo 'Complete project documentation' created",
                CorrelationId = "corr-abc123"
            }
        },
        TotalCount = 1,
        Page = 1,
        PageSize = 50
    };
}

// --- Import/Export Examples ---

public class ImportResultExample : IExamplesProvider<ImportResult>
{
    public ImportResult GetExamples() => new()
    {
        AcceptedCount = 8,
        RejectedCount = 2,
        Errors = new List<ImportError>
        {
            new() { RowNumber = 3, ClientProvidedId = "todo-3", ErrorMessage = "Title is required" },
            new() { RowNumber = 7, ClientProvidedId = "todo-7", ErrorMessage = "Invalid priority: Critical. Must be Low, Medium, or High" }
        }
    };
}
