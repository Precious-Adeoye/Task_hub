namespace TaskHub.Api.Dto
{
    public class CreateInvitationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; // "Member" or "Admin"
    }

    public class InvitationResponse
    {
        public Guid Id { get; set; }
        public Guid OrganisationId { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string InvitedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? RespondedAt { get; set; }
    }
}
