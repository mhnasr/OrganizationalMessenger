namespace OrganizationalMessenger.Domain.Entities
{
    public class SystemNotification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public bool SendImmediately { get; set; } = true;
        public bool ShowOnLogin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public User Sender { get; set; } = null!;
    }
}
