namespace TaskHub.Api.Dto
{
    public class CreateOrganisationRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class OrganisationResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class MemberResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class AddMemberRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; // "Member" or "Admin"
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}
