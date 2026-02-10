using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GroupId { get; set; }
        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public bool IsMuted { get; set; } = false;
        public DateTime? MutedUntil { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public User User { get; set; } = null!;
        public Group Group { get; set; } = null!;
    }
}
