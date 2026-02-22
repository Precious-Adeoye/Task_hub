using Microsoft.AspNetCore.Http;
using Task_hub.Application.Abstractions;
using TaskHub.Core.Enum;

namespace Task_hub.Application.Services
{
    public class OrganisationContext : IOrganisationContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStorage _storage;
        private Guid? _cachedOrganisationId;

        public OrganisationContext(IHttpContextAccessor httpContextAccessor, IStorage storage)
        {
            _httpContextAccessor = httpContextAccessor;
            _storage = storage;
        }

        public Guid? CurrentOrganisationId
        {
            get
            {
                if (_cachedOrganisationId.HasValue)
                    return _cachedOrganisationId;

                // Try to get from header
                var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Organisation-Id"].FirstOrDefault();
                if (Guid.TryParse(headerValue, out var orgId))
                {
                    _cachedOrganisationId = orgId;
                    return orgId;
                }

                // Try to get from query string
                var queryValue = _httpContextAccessor.HttpContext?.Request.Query["organisationId"].FirstOrDefault();
                if (Guid.TryParse(queryValue, out orgId))
                {
                    _cachedOrganisationId = orgId;
                    return orgId;
                }

                return null;
            }
        }

        public async Task<bool> UserIsInOrganisationAsync(Guid userId, Guid organisationId)
        {
            var membership = await _storage.GetMembershipAsync(userId, organisationId);
            return membership != null;
        }

        public async Task<bool> UserIsOrgAdminAsync(Guid userId, Guid organisationId)
        {
            var membership = await _storage.GetMembershipAsync(userId, organisationId);
            return membership?.Role == Role.OrgAdmin;
        }
    }
}
