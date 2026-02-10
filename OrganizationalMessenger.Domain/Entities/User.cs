using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrganizationalMessenger.Domain.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(256)]
        public string? PasswordHash { get; set; }

        // ==================== تصویر پروفایل ====================
        [StringLength(500)]
        public string? ProfilePicture { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; } // اضافه شد برای سازگاری با ChatController

        [StringLength(200)]
        public string? Bio { get; set; }

        // ==================== وضعیت آنلاین ====================
        public bool IsOnline { get; set; } = false;
        public DateTime? LastSeen { get; set; }
        public DateTime? LastSeenAt { get; set; }

        // ==================== احراز هویت Active Directory ====================
        [StringLength(100)]
        public string? ActiveDirectoryId { get; set; }

        public bool IsADAuthenticated { get; set; } = false;

        // ==================== احراز هویت ERP ====================
        [StringLength(50)]
        public string? ErpUserId { get; set; }

        public bool IsErpAuthenticated { get; set; } = false;

        // ==================== احراز هویت OTP ====================
        [StringLength(10)]
        public string? OtpCode { get; set; }

        public DateTime? OtpExpireTime { get; set; }
        public bool IsOtpVerified { get; set; } = false;

        // ==================== مجوزها و دسترسی‌ها ====================
        public bool CanCreateGroup { get; set; } = true;
        public bool CanCreateChannel { get; set; } = true;
        public bool CanMakeVoiceCall { get; set; } = true;
        public bool CanMakeVideoCall { get; set; } = true;
        public bool IsAdmin { get; set; } = false;

        // ==================== اعتبار پیامک ====================
        public int SmsCredit { get; set; } = 10;

        // ==================== تنظیمات VoIP ====================
        [StringLength(50)]
        public string? VoipExtension { get; set; }

        [StringLength(100)]
        public string? VoipPassword { get; set; }

        public bool VoipEnabled { get; set; } = false;

        // ==================== تنظیمات اعلان‌ها ====================
        public bool NotificationsEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public bool ShowPreview { get; set; } = true;

        // ==================== وضعیت حساب ====================
        public bool IsActive { get; set; } = true;
        public bool IsBlocked { get; set; } = false;

        [StringLength(500)]
        public string? BlockReason { get; set; }

        public DateTime? BlockedAt { get; set; }

        // ==================== کلید عمومی E2E ====================
        [StringLength(1000)]
        public string? PublicKey { get; set; }

        // ==================== Refresh Token ====================
        [StringLength(500)]
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiry { get; set; }

        // ==================== Navigation Properties ====================

        // پیام‌های ارسالی
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

        // پیام‌های دریافتی
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

        // گروه‌های ایجاد شده
        public virtual ICollection<Group> CreatedGroups { get; set; } = new List<Group>();

        // عضویت در گروه‌ها
        public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

        // کانال‌های ایجاد شده
        public virtual ICollection<Channel> CreatedChannels { get; set; } = new List<Channel>();

        // عضویت در کانال‌ها
        public virtual ICollection<UserChannel> UserChannels { get; set; } = new List<UserChannel>();

        // تماس‌های شروع شده
        public virtual ICollection<Call> InitiatedCalls { get; set; } = new List<Call>();

        // تماس‌های دریافت شده
        public virtual ICollection<Call> ReceivedCalls { get; set; } = new List<Call>();

        // فایل‌های آپلود شده
        public virtual ICollection<FileAttachment> UploadedFiles { get; set; } = new List<FileAttachment>();

        // واکنش‌ها
        public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();

        // پیام‌های ستاره‌دار
        public virtual ICollection<StarredMessage> StarredMessages { get; set; } = new List<StarredMessage>();

        // گزارش‌های پیام
        public virtual ICollection<MessageReport> MessageReports { get; set; } = new List<MessageReport>();

        // لاگ‌های ورود
        public virtual ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();

        // لاگ‌های اعتبار پیامک
        public virtual ICollection<SmsCreditLog> SmsCreditLogs { get; set; } = new List<SmsCreditLog>();

        // ==================== متدهای کمکی ====================

        /// <summary>
        /// به‌روزرسانی زمان آخرین مشاهده
        /// </summary>
        public void UpdateLastSeen()
        {
            LastSeen = DateTime.Now;
            LastSeenAt = DateTime.Now;
        }

        /// <summary>
        /// تنظیم کاربر به حالت آنلاین
        /// </summary>
        public void SetOnline()
        {
            IsOnline = true;
            UpdateLastSeen();
        }

        /// <summary>
        /// تنظیم کاربر به حالت آفلاین
        /// </summary>
        public void SetOffline()
        {
            IsOnline = false;
            UpdateLastSeen();
        }

        /// <summary>
        /// تولید کد OTP جدید
        /// </summary>
        public string GenerateOtp(int expirationMinutes = 5)
        {
            var random = new Random();
            OtpCode = random.Next(100000, 999999).ToString();
            OtpExpireTime = DateTime.Now.AddMinutes(expirationMinutes);
            IsOtpVerified = false;
            return OtpCode;
        }

        /// <summary>
        /// اعتبارسنجی کد OTP
        /// </summary>
        public bool ValidateOtp(string code)
        {
            if (string.IsNullOrEmpty(OtpCode) || OtpExpireTime == null)
                return false;

            if (OtpExpireTime < DateTime.Now)
                return false;

            if (OtpCode != code)
                return false;

            IsOtpVerified = true;
            OtpCode = null;
            OtpExpireTime = null;
            return true;
        }

        /// <summary>
        /// کاهش اعتبار پیامک
        /// </summary>
        public bool DeductSmsCredit(int amount = 1)
        {
            if (SmsCredit < amount)
                return false;

            SmsCredit -= amount;
            return true;
        }

        /// <summary>
        /// افزایش اعتبار پیامک
        /// </summary>
        public void AddSmsCredit(int amount)
        {
            SmsCredit += amount;
        }
    }
}
