using TaskHub.Core.Enum;

namespace TaskHub.Core.Entities.File_storage
{
    public class InvitationData
    {
        public Guid Id { get; set; }
        public Guid OrganisationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Role Role { get; set; }
        public Guid InvitedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
