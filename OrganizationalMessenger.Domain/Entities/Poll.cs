namespace OrganizationalMessenger.Domain.Entities
{
    public class Poll
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public int? GroupId { get; set; }
        public int? ChannelId { get; set; }
        public bool AllowMultipleAnswers { get; set; } = false;
        public bool IsAnonymous { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public User Creator { get; set; } = null!;
        public Group? Group { get; set; }
        public Channel? Channel { get; set; }
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}
