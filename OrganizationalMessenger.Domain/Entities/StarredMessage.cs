namespace OrganizationalMessenger.Domain.Entities
{
    public class StarredMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MessageId { get; set; }
        public DateTime StarredAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public User User { get; set; } = null!;
        public Message Message { get; set; } = null!;
    }
}
