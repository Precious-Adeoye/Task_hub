using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Task_hub.Application.Abstractions;

namespace Task_hub.Application.Authorization
{
    public class OrganisationAuthorizationHandler : AuthorizationHandler<OrganisationRequirement>
    {
        private readonly IOrganisationContext _organisationContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrganisationAuthorizationHandler(
            IOrganisationContext organisationContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _organisationContext = organisationContext;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OrganisationRequirement requirement)
        {
            // Get user ID
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Fail();
                return;
            }

            // Get organisation ID from route/query/header
            var orgId = _organisationContext.CurrentOrganisationId;
            if (!orgId.HasValue)
            {
                context.Fail();
                return;
            }

            // Check if user is in organisation
            var isInOrg = await _organisationContext.UserIsInOrganisationAsync(userId, orgId.Value);
            if (!isInOrg)
            {
                context.Fail();
                return;
            }

            // If admin required, check admin status
            if (requirement.RequireAdmin)
            {
                var isAdmin = await _organisationContext.UserIsOrgAdminAsync(userId, orgId.Value);
                if (!isAdmin)
                {
                    context.Fail();
                    return;
                }
            }

            context.Succeed(requirement);
        }
    }

    // Custom attribute for controller actions
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireOrganisationAttribute : AuthorizeAttribute
    {
        public bool RequireAdmin { get; set; }

        public RequireOrganisationAttribute(bool requireAdmin = false)
        {
            RequireAdmin = requireAdmin;
            Policy = requireAdmin ? "OrgAdmin" : "OrgMember";
        }
    }
}
