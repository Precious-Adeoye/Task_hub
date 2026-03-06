using TaskHub.Core.Enum;

namespace TaskHub.Core.Entities
{
    public class Invitation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganisationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Member;
        public Guid InvitedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public DateTime? RespondedAt { get; set; }

        // Navigation properties
        public Organisation Organisation { get; set; } = null!;
    }
}
