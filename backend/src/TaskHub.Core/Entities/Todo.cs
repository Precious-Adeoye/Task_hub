using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskHub.Core.Entities
{
    public class Todo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganisationId { get; set; }
        public Guid CreatedBy { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TodoStatus Status { get; set; } = TodoStatus.Open;
        public Priority Priority { get; set; } = Priority.Medium;
        public List<string> Tags { get; set; } = new();
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
        public string Version { get; set; } = Guid.NewGuid().ToString(); // For ETag/optimistic concurrency

        // Navigation properties
        public Organisation Organisation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
