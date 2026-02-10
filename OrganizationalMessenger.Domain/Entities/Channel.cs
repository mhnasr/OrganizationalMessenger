using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrganizationalMessenger.Domain.Entities
{
    /// <summary>
    /// Entity کانال - برای ارتباطات یک‌طرفه (اطلاع‌رسانی)
    /// </summary>
    public class Channel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        public int CreatorId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;

        // ==================== حذف نرم ====================
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // ==================== تنظیمات کانال ====================

        /// <summary>
        /// آیا فقط ادمین‌ها می‌توانند پیام ارسال کنند
        /// </summary>
        public bool OnlyAdminsCanPost { get; set; } = true;

        /// <summary>
        /// آیا کامنت‌گذاری فعال است
        /// </summary>
        public bool AllowComments { get; set; } = false;

        /// <summary>
        /// آیا کانال خصوصی است (نیاز به لینک دعوت)
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// لینک دعوت یکتا
        /// </summary>
        [StringLength(100)]
        public string? InviteLink { get; set; }

        // ==================== آمار ====================

        public int MemberCount { get; set; } = 0;

        // ==================== Navigation Properties ====================

        [ForeignKey(nameof(CreatorId))]
        public virtual User Creator { get; set; } = null!;

        public virtual ICollection<UserChannel> UserChannels { get; set; } = new List<UserChannel>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<Call> Calls { get; set; } = new List<Call>();

        // ==================== متدهای کمکی ====================

        /// <summary>
        /// علامت‌گذاری به عنوان حذف شده
        /// </summary>
        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.Now;
            IsActive = false;
        }

        /// <summary>
        /// بازیابی کانال حذف شده
        /// </summary>
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            IsActive = true;
        }

        /// <summary>
        /// تولید لینک دعوت جدید
        /// </summary>
        public string GenerateInviteLink()
        {
            InviteLink = Guid.NewGuid().ToString("N")[..16];
            return InviteLink;
        }

        /// <summary>
        /// به‌روزرسانی تعداد اعضا
        /// </summary>
        public void UpdateMemberCount(int count)
        {
            MemberCount = count;
            UpdatedAt = DateTime.Now;
        }
    }
}
