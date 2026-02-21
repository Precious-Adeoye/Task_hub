using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHub.Core.Enum;

namespace TaskHub.Core.Entities.File_storage
{
    public class TodoData
    {
        public Guid Id { get; set; }
        public Guid OrganisationId { get; set; }
        public Guid CreatedBy { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TodoStatus Status { get; set; }
        public Priority Priority { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}
