using Microsoft.AspNetCore.Authorization;

namespace Task_hub.Application.Authorization
{
    public class OrganisationRequirement : IAuthorizationRequirement
    {
        public bool RequireAdmin { get; }

        public OrganisationRequirement(bool requireAdmin = false)
        {
            RequireAdmin = requireAdmin;
        }
    }
}
