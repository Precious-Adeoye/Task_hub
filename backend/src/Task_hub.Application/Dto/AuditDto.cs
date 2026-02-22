namespace Task_hub.Application.Dto
{
    public class AuditQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditLogResponse
    {
        public IEnumerable<AuditEntryResponse> Logs { get; set; } = new List<AuditEntryResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AuditEntryResponse
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid ActorUserId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class AuditSummaryResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public IEnumerable<ActionTypeSummary> Actions { get; set; } = new List<ActionTypeSummary>();
    }

    public class ActionTypeSummary
    {
        public string ActionType { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastOccurrence { get; set; }
    }
}
