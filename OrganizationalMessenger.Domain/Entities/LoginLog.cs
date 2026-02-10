using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class LoginLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AuthenticationType AuthType { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccessful { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
