using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHub.Core.Enum;

namespace TaskHub.Core.Entities.File_storage
{
    public class MembershipData
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganisationId { get; set; }
        public Role Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
