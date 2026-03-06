using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Authorization;
using Task_hub.Application.Extensions;
using TaskHub.Api.Dto;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;

namespace TaskHub.Api.Controller
{
    [Route("api/v1")]
    [ApiController]
    [Produces("application/json")]
    public class InvitationController : ControllerBase
    {
        private readonly IStorage _storage;
        private readonly IAuditService _auditService;
        private readonly ILogger<InvitationController> _logger;

        public InvitationController(
            IStorage storage,
            IAuditService auditService,
            ILogger<InvitationController> logger)
        {
            _storage = storage;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpPost("organisations/{orgId}/invitations")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<InvitationResponse>> CreateInvitation(Guid orgId, CreateInvitationRequest request)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var org = await _storage.GetOrganisationByIdAsync(orgId);
            if (org == null)
                return NotFound();

            // Check if user is already a member
            var existingUser = await _storage.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                var existingMembership = await _storage.GetMembershipAsync(existingUser.Id, orgId);
                if (existingMembership != null)
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Already a member",
                        Detail = "This user is already a member of the organisation.",
                        Status = 400,
                        Instance = Request.Path
                    });
            }

            // Check for existing pending invitation
            var pendingInvitations = await _storage.GetPendingInvitationsForEmailAsync(request.Email);
            if (pendingInvitations.Any(i => i.OrganisationId == orgId))
                return BadRequest(new ProblemDetails
                {
                    Title = "Duplicate invitation",
                    Detail = "A pending invitation already exists for this email.",
                    Status = 400,
                    Instance = Request.Path
                });

            var invitation = new Invitation
            {
                OrganisationId = orgId,
                Email = request.Email,
                Role = request.Role == "Admin" ? Role.OrgAdmin : Role.Member,
                InvitedBy = userId.Value
            };

            await _storage.AddInvitationAsync(invitation);

            var inviter = await _storage.GetUserByIdAsync(userId.Value);

            await _auditService.AuditAsync("InvitationCreated", "Invitation", invitation.Id.ToString(),
                $"Invited {request.Email} as {request.Role}", userId.Value, orgId);

            return Created($"api/v1/invitations/{invitation.Id}", new InvitationResponse
            {
                Id = invitation.Id,
                OrganisationId = orgId,
                OrganisationName = org.Name,
                Email = invitation.Email,
                Role = invitation.Role.ToString(),
                InvitedByUsername = inviter?.Username ?? "Unknown",
                CreatedAt = invitation.CreatedAt,
                Status = invitation.Status.ToString()
            });
        }

        [HttpGet("organisations/{orgId}/invitations")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(typeof(IEnumerable<InvitationResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<InvitationResponse>>> GetOrganisationInvitations(Guid orgId)
        {
            var org = await _storage.GetOrganisationByIdAsync(orgId);
            if (org == null)
                return NotFound();

            var invitations = await _storage.GetOrganisationInvitationsAsync(orgId);
            var responses = new List<InvitationResponse>();

            foreach (var inv in invitations)
            {
                var inviter = await _storage.GetUserByIdAsync(inv.InvitedBy);
                responses.Add(new InvitationResponse
                {
                    Id = inv.Id,
                    OrganisationId = inv.OrganisationId,
                    OrganisationName = org.Name,
                    Email = inv.Email,
                    Role = inv.Role.ToString(),
                    InvitedByUsername = inviter?.Username ?? "Unknown",
                    CreatedAt = inv.CreatedAt,
                    Status = inv.Status.ToString(),
                    RespondedAt = inv.RespondedAt
                });
            }

            return Ok(responses);
        }

        [Authorize]
        [HttpGet("invitations/pending")]
        [ProducesResponseType(typeof(IEnumerable<InvitationResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<InvitationResponse>>> GetMyPendingInvitations()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _storage.GetUserByIdAsync(userId.Value);
            if (user == null)
                return Unauthorized();

            var invitations = await _storage.GetPendingInvitationsForEmailAsync(user.Email);
            var responses = new List<InvitationResponse>();

            foreach (var inv in invitations)
            {
                var org = await _storage.GetOrganisationByIdAsync(inv.OrganisationId);
                var inviter = await _storage.GetUserByIdAsync(inv.InvitedBy);
                responses.Add(new InvitationResponse
                {
                    Id = inv.Id,
                    OrganisationId = inv.OrganisationId,
                    OrganisationName = org?.Name ?? "Unknown",
                    Email = inv.Email,
                    Role = inv.Role.ToString(),
                    InvitedByUsername = inviter?.Username ?? "Unknown",
                    CreatedAt = inv.CreatedAt,
                    Status = inv.Status.ToString()
                });
            }

            return Ok(responses);
        }

        [Authorize]
        [HttpPost("invitations/{id}/accept")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptInvitation(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _storage.GetUserByIdAsync(userId.Value);
            if (user == null)
                return Unauthorized();

            var invitation = await _storage.GetInvitationByIdAsync(id);
            if (invitation == null)
                return NotFound();

            if (!invitation.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid invitation",
                    Detail = "This invitation is not for your email address.",
                    Status = 400,
                    Instance = Request.Path
                });

            if (invitation.Status != InvitationStatus.Pending)
                return BadRequest(new ProblemDetails
                {
                    Title = "Invitation already responded",
                    Detail = $"This invitation has already been {invitation.Status.ToString().ToLower()}.",
                    Status = 400,
                    Instance = Request.Path
                });

            // Check if already a member
            var existingMembership = await _storage.GetMembershipAsync(userId.Value, invitation.OrganisationId);
            if (existingMembership != null)
            {
                invitation.Status = InvitationStatus.Accepted;
                invitation.RespondedAt = DateTime.UtcNow;
                await _storage.UpdateInvitationAsync(invitation);
                return Ok(new { message = "You are already a member of this organisation." });
            }

            // Create membership
            var membership = new Membership
            {
                UserId = userId.Value,
                OrganisationId = invitation.OrganisationId,
                Role = invitation.Role
            };
            await _storage.AddMembershipAsync(membership);

            // Update invitation status
            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = DateTime.UtcNow;
            await _storage.UpdateInvitationAsync(invitation);

            await _auditService.AuditAsync("InvitationAccepted", "Invitation", invitation.Id.ToString(),
                $"User {user.Username} accepted invitation", userId.Value, invitation.OrganisationId);

            return Ok(new { message = "Invitation accepted. You are now a member of the organisation." });
        }

        [Authorize]
        [HttpPost("invitations/{id}/decline")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeclineInvitation(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _storage.GetUserByIdAsync(userId.Value);
            if (user == null)
                return Unauthorized();

            var invitation = await _storage.GetInvitationByIdAsync(id);
            if (invitation == null)
                return NotFound();

            if (!invitation.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid invitation",
                    Detail = "This invitation is not for your email address.",
                    Status = 400,
                    Instance = Request.Path
                });

            if (invitation.Status != InvitationStatus.Pending)
                return BadRequest(new ProblemDetails
                {
                    Title = "Invitation already responded",
                    Detail = $"This invitation has already been {invitation.Status.ToString().ToLower()}.",
                    Status = 400,
                    Instance = Request.Path
                });

            invitation.Status = InvitationStatus.Declined;
            invitation.RespondedAt = DateTime.UtcNow;
            await _storage.UpdateInvitationAsync(invitation);

            await _auditService.AuditAsync("InvitationDeclined", "Invitation", invitation.Id.ToString(),
                $"User {user.Username} declined invitation", userId.Value, invitation.OrganisationId);

            return Ok(new { message = "Invitation declined." });
        }
    }
}
