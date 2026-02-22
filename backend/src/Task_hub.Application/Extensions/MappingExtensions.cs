using TaskHub.Api.Dto;
using Task_hub.Application.Dto;
using TaskHub.Core.Entities;

namespace Task_hub.Application.Extensions;

public static class MappingExtensions
{
    public static TodoResponse ToResponse(this Todo todo)
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

    public static OrganisationResponse ToResponse(this Organisation org)
    {
        return new OrganisationResponse
        {
            Id = org.Id,
            Name = org.Name,
            CreatedAt = org.CreatedAt,
            CreatedBy = org.CreatedBy
        };
    }

    public static AuditEntryResponse ToResponse(this AuditLog log)
    {
        return new AuditEntryResponse
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            ActorUserId = log.ActorUserId,
            ActionType = log.ActionType,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Details = log.Details,
            CorrelationId = log.CorrelationId
        };
    }
}
