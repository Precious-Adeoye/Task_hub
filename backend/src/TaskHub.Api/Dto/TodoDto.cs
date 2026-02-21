using TaskHub.Core.Enum;

namespace TaskHub.Api.Dto
{
    public class TodoQuery
    {
        public TodoStatus? Status { get; set; }
        public bool? Overdue { get; set; }
        public string? Tag { get; set; }
        public bool? IncludeDeleted { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? SortBy { get; set; }
        public bool? SortDescending { get; set; }
    }

    public class CreateTodoRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public List<string> Tags { get; set; } = new();
        public DateTime? DueDate { get; set; }
    }

    public class UpdateTodoRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public Priority? Priority { get; set; }
        public List<string>? Tags { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class TodoResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}
