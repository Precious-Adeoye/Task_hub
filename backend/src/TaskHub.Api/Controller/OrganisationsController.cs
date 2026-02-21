using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstraction;
using Task_hub.Application.Authorization;
using TaskHub.Api.Dto;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;

namespace TaskHub.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganisationsController : ControllerBase
    {
        private readonly IStorage _storage;
        private readonly IOrganisationContext _organisationContext;
        private readonly ILogger<OrganisationsController> _logger;

        public OrganisationsController(
            IStorage storage,
            IOrganisationContext organisationContext,
            ILogger<OrganisationsController> logger)
        {
            _storage = storage;
            _organisationContext = organisationContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<OrganisationResponse>> CreateOrganisation(CreateOrganisationRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var organisation = new Organisation
            {
                Name = request.Name,
                CreatedBy = userId.Value
            };

            await _storage.AddOrganisationAsync(organisation);

            // Add creator as OrgAdmin
            var membership = new Membership
            {
                UserId = userId.Value,
                OrganisationId = organisation.Id,
                Role = Role.OrgAdmin
            };
            await _storage.AddMembershipAsync(membership);

            // Audit log
            await Audit("OrganisationCreated", "Organisation", organisation.Id.ToString(),
                $"Organisation '{organisation.Name}' created", userId.Value, organisation.Id);

            _logger.LogInformation("Organisation created: {OrgId} by user {UserId}", organisation.Id, userId);

            return CreatedAtAction(nameof(GetOrganisation), new { id = organisation.Id },
                MapToResponse(organisation));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrganisationResponse>>> GetMyOrganisations()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var organisations = await _storage.GetUserOrganisationsAsync(userId.Value);
            return Ok(organisations.Select(MapToResponse));
        }

        [HttpGet("{id}")]
        [RequireOrganisation]
        public async Task<ActionResult<OrganisationResponse>> GetOrganisation(Guid id)
        {
            var organisation = await _storage.GetOrganisationByIdAsync(id);
            if (organisation == null)
                return NotFound();

            return Ok(MapToResponse(organisation));
        }

        [HttpGet("{id}/members")]
        [RequireOrganisation(RequireAdmin = true)]
        public async Task<ActionResult<IEnumerable<MemberResponse>>> GetMembers(Guid id)
        {
            var memberships = await _storage.GetOrganisationMembershipsAsync(id);
            var members = new List<MemberResponse>();

            foreach (var membership in memberships)
            {
                var user = await _storage.GetUserByIdAsync(membership.UserId);
                if (user != null)
                {
                    members.Add(new MemberResponse
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = membership.Role.ToString(),
                        JoinedAt = membership.JoinedAt
                    });
                }
            }

            return Ok(members);
        }

        [HttpPost("{id}/members")]
        [RequireOrganisation(RequireAdmin = true)]
        public async Task<IActionResult> AddMember(Guid id, AddMemberRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Find user by email
            var user = await _storage.GetUserByEmailAsync(request.Email);
            if (user == null)
                return BadRequest(new { error = "User not found" });

            // Check if already a member
            var existingMembership = await _storage.GetMembershipAsync(user.Id, id);
            if (existingMembership != null)
                return BadRequest(new { error = "User is already a member" });

            // Add membership
            var membership = new Membership
            {
                UserId = user.Id,
                OrganisationId = id,
                Role = request.Role == "Admin" ? Role.OrgAdmin : Role.Member
            };
            await _storage.AddMembershipAsync(membership);

            // Audit log
            await Audit("MemberAdded", "Membership", $"{user.Id}:{id}",
                $"User {user.Username} added as {request.Role}", currentUserId.Value, id);

            _logger.LogInformation("User {UserId} added to organisation {OrgId} as {Role}",
                user.Id, id, request.Role);

            return Ok(new { message = "Member added successfully" });
        }

        [HttpPut("{id}/members/{userId}/role")]
        [RequireOrganisation(RequireAdmin = true)]
        public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, UpdateRoleRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Can't change own role (prevent lockout)
            if (userId == currentUserId.Value)
                return BadRequest(new { error = "Cannot change your own role" });

            var membership = await _storage.GetMembershipAsync(userId, id);
            if (membership == null)
                return NotFound();

            var oldRole = membership.Role;
            membership.Role = request.Role == "Admin" ? Role.OrgAdmin : Role.Member;
            await _storage.UpdateMembershipAsync(membership);

            // Audit log
            await Audit("MemberRoleChanged", "Membership", $"{userId}:{id}",
                $"Role changed from {oldRole} to {membership.Role}", currentUserId.Value, id);

            return Ok(new { message = "Role updated successfully" });
        }

        [HttpDelete("{id}/members/{userId}")]
        [RequireOrganisation(RequireAdmin = true)]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Can't remove yourself
            if (userId == currentUserId.Value)
                return BadRequest(new { error = "Cannot remove yourself" });

            var membership = await _storage.GetMembershipAsync(userId, id);
            if (membership == null)
                return NotFound();

            await _storage.RemoveMembershipAsync(userId, id);

            // Audit log
            await Audit("MemberRemoved", "Membership", $"{userId}:{id}",
                $"User {userId} removed from organisation", currentUserId.Value, id);

            return Ok(new { message = "Member removed successfully" });
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;
            return null;
        }

        private async Task Audit(string action, string entityType, string entityId, string details, Guid actorId, Guid orgId)
        {
            var auditLog = new AuditLog
            {
                ActorUserId = actorId,
                OrganisationId = orgId,
                ActionType = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CorrelationId = HttpContext.TraceIdentifier
            };
            await _storage.AddAuditLogAsync(auditLog);
        }

        private OrganisationResponse MapToResponse(Organisation org)
        {
            return new OrganisationResponse
            {
                Id = org.Id,
                Name = org.Name,
                CreatedAt = org.CreatedAt,
                CreatedBy = org.CreatedBy
            };
        }
    }
}
