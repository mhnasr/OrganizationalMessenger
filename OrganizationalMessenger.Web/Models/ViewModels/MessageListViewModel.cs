namespace OrganizationalMessenger.Web.Models.ViewModels
{
    /// <summary>
    /// ViewModel برای نمایش لیست پیام‌ها در پنل ادمین
    /// </summary>
    public class MessageListViewModel
    {
        public int Id { get; set; }

        public int SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string? SenderUsername { get; set; }

        public string? SenderAvatar { get; set; }

        public string MessageText { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public bool IsEdited { get; set; }

        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; }

        public string Type { get; set; } = "Text";

        public string? AttachmentUrl { get; set; }

        public string? AttachmentName { get; set; }

        public long? AttachmentSize { get; set; }

        public int ReplyCount { get; set; }

        public int ReactionCount { get; set; }

        public bool IsRead { get; set; }

        public bool IsDelivered { get; set; }
    }
}
