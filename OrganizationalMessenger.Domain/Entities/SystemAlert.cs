using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class SystemAlert
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public AlertType Type { get; set; } = AlertType.Info;
        public AlertDeliveryType DeliveryType { get; set; } = AlertDeliveryType.Immediate;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public int CreatedByAdminId { get; set; }

        // Navigation
        public AdminUser CreatedByAdmin { get; set; } = null!;
        public ICollection<SystemAlertRead> ReadBy { get; set; } = new List<SystemAlertRead>();
    }

    public class SystemAlertRead
    {
        public int Id { get; set; }
        public int AlertId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.Now;

        // Navigation
        public SystemAlert Alert { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
