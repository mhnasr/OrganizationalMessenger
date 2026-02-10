// Path: OrganizationalMessenger.Web/Areas/Admin/Models/SettingsViewModels.cs

using System.ComponentModel.DataAnnotations;
using OrganizationalMessenger.Domain.Entities;

namespace OrganizationalMessenger.Web.Areas.Admin.Models
{
    #region ========== Index ViewModel ==========

    public class SettingsIndexViewModel
    {
        public List<SystemSetting> AllSettings { get; set; } = new();
        public Dictionary<string, int> SettingCounts { get; set; } = new();
        public DateTime? LastUpdated { get; set; }
        public string? LastUpdatedBy { get; set; }

        // آمار سریع
        public int TotalUsers { get; set; }
        public int TotalMessages { get; set; }
        public int TodayActiveUsers { get; set; }
        public string SystemVersion { get; set; } = "1.0.0";
    }

    #endregion

    #region ========== General Settings ==========

    public class GeneralSettingsViewModel
    {
        [Display(Name = "نام سازمان")]
        [Required(ErrorMessage = "نام سازمان الزامی است")]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "آدرس لوگوی سازمان")]
        public string? CompanyLogoUrl { get; set; }

        [Display(Name = "امکان ایجاد گروه توسط همه کاربران")]
        public bool AllUsersCanCreateGroup { get; set; }

        [Display(Name = "امکان ایجاد کانال توسط همه کاربران")]
        public bool AllUsersCanCreateChannel { get; set; }

        [Display(Name = "حداکثر اعضای گروه")]
        [Range(2, 10000)]
        public int MaxGroupMembers { get; set; } = 200;

        [Display(Name = "حداکثر اعضای کانال")]
        [Range(1, 100000)]
        public int MaxChannelMembers { get; set; } = 5000;

        [Display(Name = "اعتبار پیامک پیش‌فرض برای کاربران جدید")]
        [Range(0, 1000)]
        public int DefaultSmsCredit { get; set; } = 10;
    }

    #endregion

    #region ========== Authentication Settings ==========

    public class AuthenticationSettingsViewModel
    {
        [Display(Name = "روش احراز هویت")]
        public string AuthType { get; set; } = "Database";

        // Active Directory Settings
        [Display(Name = "آدرس سرور AD")]
        public string? AdServer { get; set; }

        [Display(Name = "دامنه AD")]
        public string? AdDomain { get; set; }

        // ERP Settings
        [Display(Name = "آدرس API سیستم ERP")]
        [Url(ErrorMessage = "آدرس URL معتبر وارد کنید")]
        public string? ErpApiUrl { get; set; }

        [Display(Name = "کلید API سیستم ERP")]
        public string? ErpApiKey { get; set; }

        // SMS/OTP Settings
        [Display(Name = "آدرس سرویس‌دهنده پیامک")]
        public string? SmsProviderUrl { get; set; }

        [Display(Name = "کلید API پیامک")]
        public string? SmsApiKey { get; set; }

        [Display(Name = "شماره ارسال‌کننده پیامک")]
        public string? SmsSenderNumber { get; set; }

        [Display(Name = "مدت اعتبار کد OTP (دقیقه)")]
        [Range(1, 10)]
        public int OtpExpirationMinutes { get; set; } = 2;
    }

    #endregion

    #region ========== File Upload Settings ==========

    public class FileUploadSettingsViewModel
    {
        public List<FileUploadSettingItemViewModel> FileTypes { get; set; } = new();

        // تنظیمات کلی از SystemSettings
        [Display(Name = "حداکثر حجم تصویر (MB)")]
        public int MaxImageSizeMB { get; set; } = 10;

        [Display(Name = "حداکثر حجم ویدیو (MB)")]
        public int MaxVideoSizeMB { get; set; } = 100;

        [Display(Name = "حداکثر حجم فایل (MB)")]
        public int MaxFileSizeMB { get; set; } = 50;
    }

    public class FileUploadSettingItemViewModel
    {
        public int Id { get; set; }

        [Display(Name = "نوع فایل")]
        public string FileType { get; set; } = string.Empty;

        [Display(Name = "دسته‌بندی")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "حداکثر حجم (بایت)")]
        public long MaxSize { get; set; }

        [Display(Name = "مجاز")]
        public bool IsAllowed { get; set; }

        // محاسبه‌شده
        public double MaxSizeMB => Math.Round(MaxSize / (1024.0 * 1024.0), 2);

        public string CategoryIcon => Category?.ToLower() switch
        {
            "image" => "fa-image text-success",
            "video" => "fa-video text-primary",
            "audio" => "fa-music text-warning",
            "document" => "fa-file-alt text-info",
            _ => "fa-file text-secondary"
        };
    }

    #endregion

    #region ========== Message Settings ==========

    public class MessageSettingsViewModel
    {
        [Display(Name = "امکان ویرایش پیام")]
        public bool MessageEditEnabled { get; set; } = true;

        [Display(Name = "امکان حذف پیام")]
        public bool MessageDeleteEnabled { get; set; } = true;

        [Display(Name = "مهلت ویرایش پیام (دقیقه)")]
        [Range(0, 1440)]
        public int MessageEditTimeLimit { get; set; } = 15;

        [Display(Name = "مهلت حذف پیام (دقیقه)")]
        [Range(0, 1440)]
        public int MessageDeleteTimeLimit { get; set; } = 60;

        [Display(Name = "حذف پیام بعد از خواندن توسط گیرنده")]
        public bool MessageDeleteAfterRead { get; set; } = false;

        [Display(Name = "ادمین می‌تواند پیام‌ها را حذف کند")]
        public bool AdminCanDeleteMessages { get; set; } = true;

        [Display(Name = "رمزنگاری پیام‌ها")]
        public bool EncryptionEnabled { get; set; } = false;
    }

    #endregion

    #region ========== VoIP Settings ==========

    public class VoIPSettingsViewModel
    {
        [Display(Name = "فعال بودن تماس صوتی/تصویری")]
        public bool VoipEnabled { get; set; }

        [Display(Name = "آدرس سرور VoIP")]
        [Url(ErrorMessage = "آدرس URL معتبر وارد کنید")]
        public string? VoipServerUrl { get; set; }

        [Display(Name = "نام کاربری")]
        public string? VoipUsername { get; set; }

        [Display(Name = "رمز عبور")]
        public string? VoipPassword { get; set; }

        [Display(Name = "پروتکل")]
        public string VoipProtocol { get; set; } = "WebRTC";
    }

    #endregion

    #region ========== Telegram Settings ==========

    public class TelegramSettingsViewModel
    {
        [Display(Name = "فعال بودن ربات تلگرام")]
        public bool TelegramEnabled { get; set; }

        [Display(Name = "توکن ربات تلگرام")]
        public string? TelegramBotToken { get; set; }

        // اطلاعات نمایشی (فقط خواندنی)
        public string? BotUsername { get; set; }
        public bool IsConnected { get; set; }
    }

    #endregion

    #region ========== SMS Settings ==========

    public class SmsSettingsViewModel
    {
        [Display(Name = "آدرس سرویس‌دهنده")]
        [Url(ErrorMessage = "آدرس URL معتبر وارد کنید")]
        public string? SmsProviderUrl { get; set; }

        [Display(Name = "کلید API")]
        public string? SmsApiKey { get; set; }

        [Display(Name = "شماره ارسال‌کننده")]
        public string? SmsSenderNumber { get; set; }

        [Display(Name = "مدت اعتبار OTP (دقیقه)")]
        [Range(1, 10)]
        public int OtpExpirationMinutes { get; set; } = 2;

        // آمار
        public int TodaySentCount { get; set; }
        public decimal? CurrentBalance { get; set; }
    }

    #endregion

    #region ========== Service Links Settings ==========

    public class ServiceLinksViewModel
    {
        public List<ServiceLink> Links { get; set; } = new();
    }

    public class ServiceLinkFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان الزامی است")]
        [StringLength(100)]
        [Display(Name = "عنوان")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "آدرس الزامی است")]
        [Url(ErrorMessage = "آدرس URL معتبر وارد کنید")]
        [Display(Name = "آدرس لینک")]
        public string Url { get; set; } = string.Empty;

        [Display(Name = "آیکون")]
        public string? IconUrl { get; set; }

        [Display(Name = "ترتیب نمایش")]
        public int DisplayOrder { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;
    }

    #endregion

    #region ========== Forbidden Words Settings ==========

    public class ForbiddenWordsViewModel
    {
        public List<ForbiddenWord> Words { get; set; } = new();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }

        // فرم افزودن
        [Required(ErrorMessage = "کلمه الزامی است")]
        [StringLength(100)]
        public string NewWord { get; set; } = string.Empty;
    }

    #endregion

    #region ========== System Info ==========

    public class SystemInfoViewModel
    {
        public string DotNetVersion { get; set; } = string.Empty;
        public string OSDescription { get; set; } = string.Empty;
        public long MemoryUsageMB { get; set; }
        public long TotalMemoryMB { get; set; }
        public int ProcessorCount { get; set; }
        public TimeSpan Uptime { get; set; }
        public string MachineName { get; set; } = string.Empty;

        // آمار اپلیکیشن
        public int TotalUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public long DatabaseSizeMB { get; set; }
        public long FileStorageSizeMB { get; set; }
    }

    #endregion
}
