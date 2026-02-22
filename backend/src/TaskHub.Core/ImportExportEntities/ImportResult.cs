using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskHub.Core.ImportExportEntities
{
    public class ImportResult
    {
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
        public List<ImportError> Errors { get; set; } = new();
    }

    public class ImportError
    {
        public int RowNumber { get; set; }
        public string? ClientProvidedId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ImportOptions
    {
        public bool Idempotent { get; set; } = true;
        public bool OverwriteExisting { get; set; } = false;
    }
}
