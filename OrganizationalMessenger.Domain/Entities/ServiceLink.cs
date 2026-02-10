namespace OrganizationalMessenger.Domain.Entities
{
    public class ServiceLink
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string? IconClass { get; set; } // برای Font Awesome
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool OpenInNewTab { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int CreatedByAdminId { get; set; }

        // Navigation
        public AdminUser CreatedByAdmin { get; set; } = null!;
    }
}
