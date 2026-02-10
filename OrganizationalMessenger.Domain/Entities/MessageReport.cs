namespace OrganizationalMessenger.Domain.Entities
{
    public class MessageReport
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int ReporterId { get; set; }
        public string? Description { get; set; }
        public bool IsReviewed { get; set; } = false;
        public int? ReviewedByAdminId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public Message Message { get; set; } = null!;
        public User Reporter { get; set; } = null!;
        public User? ReviewedByAdmin { get; set; }
    }
}
