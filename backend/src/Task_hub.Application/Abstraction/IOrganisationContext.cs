using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_hub.Application.Abstraction
{
    public interface IOrganisationContext
    {
        Guid? CurrentOrganisationId { get; }
        Task<bool> UserIsInOrganisationAsync(Guid userId, Guid organisationId);
        Task<bool> UserIsOrgAdminAsync(Guid userId, Guid organisationId);
    }
}
