using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class UserGroup
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GroupId { get; set; }

        // نقش کاربر در گروه
        public GroupRole Role { get; set; } = GroupRole.Member;
        public bool IsAdmin { get; set; } = false; // ⭐ اضافه شد

        // تنظیمات
        public bool IsMuted { get; set; } = false;
        public DateTime? MutedUntil { get; set; }

        // زمان‌ها
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public User User { get; set; } = null!;
        public Group Group { get; set; } = null!;
    }
}
