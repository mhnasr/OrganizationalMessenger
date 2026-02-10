namespace OrganizationalMessenger.Domain.Entities
{
    public class TelegramMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TelegramChatId { get; set; } = string.Empty;
        public string? TelegramUsername { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool IsIncoming { get; set; } // true = از تلگرام به سیستم
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
