namespace OrganizationalMessenger.Domain.Entities
{
    public class SmsCreditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Amount { get; set; } // مثبت: شارژ، منفی: مصرف
        public string Description { get; set; } = string.Empty;
        public int? RelatedMessageId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public User User { get; set; } = null!;
        public Message? RelatedMessage { get; set; }
    }
}
