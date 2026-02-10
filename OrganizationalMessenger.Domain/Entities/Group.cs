namespace OrganizationalMessenger.Domain.Entities
{
    public class Group : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public int CreatorId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = false;
        public int MaxMembers { get; set; } = 200;

        // Navigation Properties
        public User Creator { get; set; } = null!;
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<Call> Calls { get; set; } = new List<Call>();
    }
}
