using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class SmsCreditRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RequestedAmount { get; set; }
        public string? Reason { get; set; }
        public SmsCreditRequestStatus Status { get; set; } = SmsCreditRequestStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedByAdminId { get; set; }
        public int? ApprovedAmount { get; set; }
        public string? AdminNote { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public AdminUser? ProcessedByAdmin { get; set; }
    }
}
