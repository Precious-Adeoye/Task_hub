using Microsoft.AspNetCore.Http;
using Task_hub.Application.Abstractions;
using TaskHub.Core.Entities;

namespace Task_hub.Application.Services;

public class AuditService : IAuditService
{
    private readonly IStorage _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IStorage storage, IHttpContextAccessor httpContextAccessor)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task AuditAsync(string action, string entityType, string entityId, string details, Guid actorId, Guid orgId)
    {
        var auditLog = new AuditLog
        {
            ActorUserId = actorId,
            OrganisationId = orgId,
            ActionType = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CorrelationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
                           ?? _httpContextAccessor.HttpContext?.TraceIdentifier
                           ?? string.Empty
        };
        await _storage.AddAuditLogAsync(auditLog);
    }
}
