using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    /// <summary>
    /// جدول واسط برای ارتباط Many-to-Many بین User و Channel
    /// </summary>
    public class UserChannel
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int ChannelId { get; set; }

        // ==================== نقش و دسترسی‌ها ====================

        /// <summary>
        /// نقش کاربر در کانال
        /// </summary>
        public ChannelRole Role { get; set; } = ChannelRole.Subscriber;

        /// <summary>
        /// آیا کاربر ادمین کانال است
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// آیا کاربر مالک/سازنده کانال است
        /// </summary>
        public bool IsOwner { get; set; } = false;

        /// <summary>
        /// آیا کاربر می‌تواند پیام ارسال کند
        /// </summary>
        public bool CanPost { get; set; } = false;

        /// <summary>
        /// آیا کاربر می‌تواند پیام حذف کند
        /// </summary>
        public bool CanDeleteMessages { get; set; } = false;

        /// <summary>
        /// آیا کاربر می‌تواند اعضا را مدیریت کند
        /// </summary>
        public bool CanManageMembers { get; set; } = false;

        // ==================== وضعیت عضویت ====================

        /// <summary>
        /// آیا عضویت فعال است
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاریخ عضویت
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// تاریخ لغو عضویت
        /// </summary>
        public DateTime? LeftAt { get; set; }

        // ==================== تنظیمات اعلان ====================

        /// <summary>
        /// آیا اعلان‌های کانال برای کاربر فعال است
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// آیا کانال برای کاربر بی‌صدا شده
        /// </summary>
        public bool IsMuted { get; set; } = false;

        /// <summary>
        /// تاریخ پایان بی‌صدا بودن
        /// </summary>
        public DateTime? MutedUntil { get; set; }

        /// <summary>
        /// آیا کانال پین شده است
        /// </summary>
        public bool IsPinned { get; set; } = false;

        // ==================== وضعیت خواندن پیام‌ها ====================

        /// <summary>
        /// آخرین پیام خوانده شده
        /// </summary>
        public long? LastReadMessageId { get; set; }

        /// <summary>
        /// تعداد پیام‌های خوانده نشده
        /// </summary>
        public int UnreadCount { get; set; } = 0;

        // ==================== Navigation Properties ====================

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [ForeignKey(nameof(ChannelId))]
        public virtual Channel Channel { get; set; } = null!;

        // ==================== متدهای کمکی ====================

        /// <summary>
        /// ترک کانال
        /// </summary>
        public void Leave()
        {
            IsActive = false;
            LeftAt = DateTime.Now;
        }

        /// <summary>
        /// پیوستن مجدد به کانال
        /// </summary>
        public void Rejoin()
        {
            IsActive = true;
            LeftAt = null;
            JoinedAt = DateTime.Now;
        }

        /// <summary>
        /// بی‌صدا کردن کانال
        /// </summary>
        public void Mute(DateTime? until = null)
        {
            IsMuted = true;
            MutedUntil = until;
            NotificationsEnabled = false;
        }

        /// <summary>
        /// فعال کردن صدای کانال
        /// </summary>
        public void Unmute()
        {
            IsMuted = false;
            MutedUntil = null;
            NotificationsEnabled = true;
        }

        /// <summary>
        /// علامت‌گذاری همه پیام‌ها به عنوان خوانده شده
        /// </summary>
        public void MarkAllAsRead(long lastMessageId)
        {
            LastReadMessageId = lastMessageId;
            UnreadCount = 0;
        }

        /// <summary>
        /// افزایش تعداد پیام‌های خوانده نشده
        /// </summary>
        public void IncrementUnread()
        {
            UnreadCount++;
        }

        /// <summary>
        /// ارتقا به ادمین
        /// </summary>
        public void PromoteToAdmin()
        {
            IsAdmin = true;
            CanPost = true;
            CanDeleteMessages = true;
            CanManageMembers = true;
            Role = ChannelRole.Admin;
        }

        /// <summary>
        /// تنزل از ادمین
        /// </summary>
        public void DemoteFromAdmin()
        {
            IsAdmin = false;
            CanDeleteMessages = false;
            CanManageMembers = false;
            Role = CanPost ? ChannelRole.Publisher : ChannelRole.Subscriber;
        }

        /// <summary>
        /// تنظیم به عنوان مالک
        /// </summary>
        public void SetAsOwner()
        {
            IsOwner = true;
            IsAdmin = true;
            CanPost = true;
            CanDeleteMessages = true;
            CanManageMembers = true;
            Role = ChannelRole.Owner;
        }
    }
}
