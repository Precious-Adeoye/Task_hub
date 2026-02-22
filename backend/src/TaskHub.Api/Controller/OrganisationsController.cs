using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Authorization;
using Task_hub.Application.Extensions;
using TaskHub.Api.Dto;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;

namespace TaskHub.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class OrganisationsController : ControllerBase
    {
        private readonly IStorage _storage;
        private readonly IAuditService _auditService;
        private readonly IOrganisationContext _organisationContext;
        private readonly ILogger<OrganisationsController> _logger;

        public OrganisationsController(
            IStorage storage,
            IAuditService auditService,
            IOrganisationContext organisationContext,
            ILogger<OrganisationsController> logger)
        {
            _storage = storage;
            _auditService = auditService;
            _organisationContext = organisationContext;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrganisationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<OrganisationResponse>> CreateOrganisation(CreateOrganisationRequest request)
        {
            var userId = User.GetUserId();
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

            await _auditService.AuditAsync("OrganisationCreated", "Organisation", organisation.Id.ToString(),
                $"Organisation '{organisation.Name}' created", userId.Value, organisation.Id);

            _logger.LogInformation("Organisation created: {OrgId} by user {UserId}", organisation.Id, userId);

            return CreatedAtAction(nameof(GetOrganisation), new { id = organisation.Id },
                organisation.ToResponse());
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrganisationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<OrganisationResponse>>> GetMyOrganisations()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var organisations = await _storage.GetUserOrganisationsAsync(userId.Value);
            return Ok(organisations.Select(o => o.ToResponse()));
        }

        [HttpGet("{id}")]
        [RequireOrganisation]
        [ProducesResponseType(typeof(OrganisationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrganisationResponse>> GetOrganisation(Guid id)
        {
            var organisation = await _storage.GetOrganisationByIdAsync(id);
            if (organisation == null)
                return NotFound();

            return Ok(organisation.ToResponse());
        }

        [HttpGet("{id}/members")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(typeof(IEnumerable<MemberResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddMember(Guid id, AddMemberRequest request)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Find user by email
            var user = await _storage.GetUserByEmailAsync(request.Email);
            if (user == null)
                return BadRequest(new ProblemDetails
                {
                    Title = "User not found",
                    Detail = "No registered user with this email address.",
                    Status = 400,
                    Instance = Request.Path
                });

            // Check if already a member
            var existingMembership = await _storage.GetMembershipAsync(user.Id, id);
            if (existingMembership != null)
                return BadRequest(new ProblemDetails
                {
                    Title = "Duplicate membership",
                    Detail = "User is already a member of this organisation.",
                    Status = 400,
                    Instance = Request.Path
                });

            // Add membership
            var membership = new Membership
            {
                UserId = user.Id,
                OrganisationId = id,
                Role = request.Role == "Admin" ? Role.OrgAdmin : Role.Member
            };
            await _storage.AddMembershipAsync(membership);

            await _auditService.AuditAsync("MemberAdded", "Membership", $"{user.Id}:{id}",
                $"User {user.Username} added as {request.Role}", currentUserId.Value, id);

            _logger.LogInformation("User {UserId} added to organisation {OrgId} as {Role}",
                user.Id, id, request.Role);

            return Ok(new { message = "Member added successfully" });
        }

        [HttpPut("{id}/members/{userId}/role")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, UpdateRoleRequest request)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Can't change own role (prevent lockout)
            if (userId == currentUserId.Value)
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid operation",
                    Detail = "Cannot change your own role.",
                    Status = 400,
                    Instance = Request.Path
                });

            var membership = await _storage.GetMembershipAsync(userId, id);
            if (membership == null)
                return NotFound();

            var oldRole = membership.Role;
            membership.Role = request.Role == "Admin" ? Role.OrgAdmin : Role.Member;
            await _storage.UpdateMembershipAsync(membership);

            await _auditService.AuditAsync("MemberRoleChanged", "Membership", $"{userId}:{id}",
                $"Role changed from {oldRole} to {membership.Role}", currentUserId.Value, id);

            return Ok(new { message = "Role updated successfully" });
        }

        [HttpDelete("{id}/members/{userId}")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Can't remove yourself
            if (userId == currentUserId.Value)
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid operation",
                    Detail = "Cannot remove yourself from the organisation.",
                    Status = 400,
                    Instance = Request.Path
                });

            var membership = await _storage.GetMembershipAsync(userId, id);
            if (membership == null)
                return NotFound();

            await _storage.RemoveMembershipAsync(userId, id);

            await _auditService.AuditAsync("MemberRemoved", "Membership", $"{userId}:{id}",
                $"User {userId} removed from organisation", currentUserId.Value, id);

            return Ok(new { message = "Member removed successfully" });
        }
    }
}
