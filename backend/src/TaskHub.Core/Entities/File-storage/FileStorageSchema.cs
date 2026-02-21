using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskHub.Core.Entities.File_storage
{
    public class FileStorageSchema
    {
        public int SchemaVersion { get; set; }
        public DateTime LastModified { get; set; }
        public Dictionary<Guid, UserData> Users { get; set; } = new();
        public Dictionary<Guid, OrganisationData> Organisations { get; set; } = new();
        public Dictionary<string, MembershipData> Memberships { get; set; } = new(); // Key: $"{userId}:{orgId}"
        public Dictionary<Guid, TodoData> Todos { get; set; } = new();
        public Dictionary<Guid, AuditLogData> AuditLogs { get; set; } = new();
    }
}
