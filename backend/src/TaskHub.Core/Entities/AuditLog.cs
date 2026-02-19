using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskHub.Core.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid ActorUserId { get; set; }
        public Guid? OrganisationId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string CorrelationId { get; set; } = string.Empty;

        // Navigation properties
        public User Actor { get; set; } = null!;
        public Organisation? Organisation { get; set; }
    }
}
