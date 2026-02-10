namespace OrganizationalMessenger.Domain.Entities
{
    public class MessageRead
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public Message Message { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
