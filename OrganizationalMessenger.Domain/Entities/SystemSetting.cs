namespace OrganizationalMessenger.Domain.Entities
{
    public class SystemSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int? UpdatedByAdminId { get; set; }

        // Navigation
        public AdminUser? UpdatedByAdmin { get; set; }
    }

    /// <summary>
    /// کلیدهای استاندارد تنظیمات - برای استفاده در کد
    /// </summary>
    public static class SettingKeys
    {
        // احراز هویت
        public const string AuthType = "Auth.Type";
        public const string ADServer = "Auth.AD.Server";
        public const string ADDomain = "Auth.AD.Domain";
        public const string ERPApiUrl = "Auth.ERP.ApiUrl";
        public const string ERPApiKey = "Auth.ERP.ApiKey";
        public const string SMSProviderUrl = "Auth.SMS.ProviderUrl";
        public const string SMSApiKey = "Auth.SMS.ApiKey";
        public const string SMSSenderNumber = "Auth.SMS.SenderNumber";
        public const string OTPExpirationMinutes = "Auth.OTP.ExpirationMinutes";

        // VoIP
        public const string VoIPEnabled = "VoIP.Enabled";
        public const string VoIPServerUrl = "VoIP.ServerUrl";
        public const string VoIPUsername = "VoIP.Username";
        public const string VoIPPassword = "VoIP.Password";
        public const string VoIPProtocol = "VoIP.Protocol";

        // تلگرام
        public const string TelegramEnabled = "Telegram.Enabled";
        public const string TelegramBotToken = "Telegram.BotToken";

        // فایل
        public const string MaxImageSize = "File.MaxImageSize";
        public const string MaxVideoSize = "File.MaxVideoSize";
        public const string MaxFileSize = "File.MaxFileSize";
        public const string AllowedImageTypes = "File.AllowedImageTypes";
        public const string AllowedVideoTypes = "File.AllowedVideoTypes";
        public const string AllowedFileTypes = "File.AllowedFileTypes";

        // پیام
        public const string MessageEditEnabled = "Message.EditEnabled";
        public const string MessageDeleteEnabled = "Message.DeleteEnabled";
        public const string MessageEditTimeLimit = "Message.EditTimeLimit";
        public const string MessageDeleteTimeLimit = "Message.DeleteTimeLimit";
        public const string MessageDeleteAfterRead = "Message.DeleteAfterRead";
        public const string AdminCanDeleteMessages = "Message.AdminCanDelete";
        public const string EncryptionEnabled = "Message.EncryptionEnabled";
        public const string ShowDeletedMessageNotice = "Message.ShowDeletedMessageNotice ";

        // گروه و کانال
        public const string AllUsersCanCreateGroup = "Group.AllCanCreate";
        public const string AllUsersCanCreateChannel = "Channel.AllCanCreate";
        public const string MaxGroupMembers = "Group.MaxMembers";
        public const string MaxChannelMembers = "Channel.MaxMembers";

        // عمومی
        public const string CompanyName = "General.CompanyName";
        public const string CompanyLogo = "General.CompanyLogo";
        public const string DefaultSmsCredit = "General.DefaultSmsCredit";
    }
}
