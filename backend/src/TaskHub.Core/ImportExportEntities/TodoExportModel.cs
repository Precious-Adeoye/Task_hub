using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskHub.Core.ImportExportEntities
{
    public class TodoExportModel
    {
        public string? ClientProvidedId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Medium";
        public List<string> Tags { get; set; } = new();
        public DateTime? DueDate { get; set; }
    }
}
