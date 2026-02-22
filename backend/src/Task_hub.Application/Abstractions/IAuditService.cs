namespace Task_hub.Application.Abstractions;

public interface IAuditService
{
    Task AuditAsync(string action, string entityType, string entityId, string details, Guid actorId, Guid orgId);
}
