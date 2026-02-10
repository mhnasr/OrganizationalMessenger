// Path: OrganizationalMessenger.Domain/Entities/Report.cs

using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class Report
    {
        public int Id { get; set; }

        // گزارش‌دهنده
        public int ReporterId { get; set; }

        // نوع آیتم گزارش شده
        public ReportItemType ItemType { get; set; }

        // شناسه آیتم گزارش شده
        public int? ReportedUserId { get; set; }
        public int? ReportedMessageId { get; set; }
        public int? ReportedGroupId { get; set; }
        public int? ReportedChannelId { get; set; }

        // جزئیات
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }

        // وضعیت
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        // پاسخ ادمین
        public long? ReviewedByAdminId { get; set; }
        public string? AdminNote { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // زمان‌ها
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public User Reporter { get; set; } = null!;
        public User? ReportedUser { get; set; }
        public Message? ReportedMessage { get; set; }
        public Group? ReportedGroup { get; set; }
        public Channel? ReportedChannel { get; set; }
        public User? ReviewedByAdmin { get; set; }
    }
}
