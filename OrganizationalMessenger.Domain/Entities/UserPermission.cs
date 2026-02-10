namespace OrganizationalMessenger.Domain.Entities
{
    public class UserPermission
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // مجوزهای گروه و کانال
        public bool CanCreateGroup { get; set; } = false;
        public bool CanCreateChannel { get; set; } = false;
        public bool CanCreatePoll { get; set; } = false;

        // مجوزهای ارتباطی
        public bool CanSendBroadcast { get; set; } = false; // آلرت به همه
        public bool CanUseTelegram { get; set; } = false;
        public bool CanUseVoIP { get; set; } = false;

        // اعتبار پیامک
        public int SmsCredit { get; set; } = 0;
        public int SmsCreditUsedThisYear { get; set; } = 0;
        public int SmsCreditYear { get; set; } = DateTime.Now.Year;

        // شماره داخلی VoIP
        public string? VoIPExtension { get; set; }

        // تاریخ‌ها
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}
