using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class ReportedMessage
    {
        public int Id { get; set; }
        public long MessageId { get; set; }
        public int ReporterId { get; set; } // کسی که گزارش داده
        public string? Reason { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByAdminId { get; set; }
        public string? AdminNote { get; set; }

        // Navigation
        public Message Message { get; set; } = null!;
        public User Reporter { get; set; } = null!;
        public AdminUser? ReviewedByAdmin { get; set; }
    }
}
