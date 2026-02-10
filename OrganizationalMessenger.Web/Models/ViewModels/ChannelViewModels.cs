using System.ComponentModel.DataAnnotations;

namespace OrganizationalMessenger.Web.Models.ViewModels
{
    // ==================== لیست کانال‌ها ====================
    public class ChannelListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== ایجاد کانال ====================
    public class ChannelCreateViewModel
    {
        [Required(ErrorMessage = "نام کانال الزامی است")]
        [StringLength(100, ErrorMessage = "نام کانال نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        [Display(Name = "نام کانال")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "انتخاب سازنده الزامی است")]
        [Display(Name = "سازنده")]
        public int CreatorId { get; set; }

        [Display(Name = "کانال عمومی")]
        public bool IsPublic { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;
    }

    // ==================== ویرایش کانال ====================
    public class ChannelEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام کانال الزامی است")]
        [StringLength(100, ErrorMessage = "نام کانال نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        [Display(Name = "نام کانال")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "کانال عمومی")]
        public bool IsPublic { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; }

        // فقط برای نمایش (ReadOnly)
        public int CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== جزئیات کانال ====================
    public class ChannelDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public int SubscriberCount { get; set; }
        public int AdminCount { get; set; }
        public int PublisherCount { get; set; }
        public int MessageCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }

        // لیست اعضای کانال
        public List<ChannelSubscriberViewModel> Subscribers { get; set; } = new();
    }

    // ==================== اعضای کانال ====================
    public class ChannelSubscriberViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsAdmin { get; set; }
        public bool CanPost { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsOwner { get; set; }
    }

    // ==================== افزودن عضو به کانال ====================
    public class AddChannelSubscriberViewModel
    {
        [Required(ErrorMessage = "انتخاب کانال الزامی است")]
        public int ChannelId { get; set; }

        [Required(ErrorMessage = "انتخاب کاربر الزامی است")]
        [Display(Name = "کاربر")]
        public int UserId { get; set; }

        [Display(Name = "دسترسی ارسال پست")]
        public bool CanPost { get; set; }

        [Display(Name = "دسترسی مدیریت")]
        public bool IsAdmin { get; set; }
    }

    // ==================== مدیریت اعضای کانال ====================
    public class ChannelSubscribersPageViewModel
    {
        public int ChannelId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public List<ChannelSubscriberViewModel> Subscribers { get; set; } = new();
        public List<AvailableUserViewModel> AvailableUsers { get; set; } = new();
    }

    // ==================== کاربران قابل افزودن ====================
    public class AvailableUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    // ==================== پیام‌های کانال ====================
    public class ChannelMessageListViewModel
    {
        public int ChannelId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public List<ChannelMessageViewModel> Messages { get; set; } = new();
        public int TotalMessages { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class ChannelMessageViewModel
    {
        public long Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool HasAttachment { get; set; }
        public int ViewCount { get; set; }
        public bool IsEdited { get; set; }
        public bool IsPinned { get; set; }
    }

    // ==================== آمار کانال ====================
    public class ChannelStatisticsViewModel
    {
        public int ChannelId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int WeekMessages { get; set; }
        public int MonthMessages { get; set; }
        public int TotalViews { get; set; }
        public double AverageViewsPerMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public List<DailyStatViewModel> DailyStats { get; set; } = new();
    }

    public class DailyStatViewModel
    {
        public DateTime Date { get; set; }
        public int MessageCount { get; set; }
        public int NewSubscribers { get; set; }
    }
}
