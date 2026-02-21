using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHub.Core.Enum;

namespace TaskHub.Core.Entities
{
    public class TodoFilter
    {
        public TodoStatus? Status { get; set; }
        public bool? Overdue { get; set; }
        public string? Tag { get; set; }
        public bool IncludeDeleted { get; set; } = false;

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;
    }
}
