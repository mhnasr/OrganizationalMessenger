using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class Message : BaseEntity
    {
        // فرستنده و گیرنده
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; } // برای پیام خصوصی

        // گروه یا کانال
        public int? GroupId { get; set; }
        public int? ChannelId { get; set; }

        // محتوای پیام
        public string? MessageText { get; set; } // ⭐ اضافه شد - نام قدیمی
        public string? Content { get; set; } // نام جدید (هر دو را نگه می‌داریم)
        public string? EncryptedContent { get; set; }

        // نوع پیام
        public MessageType Type { get; set; } = MessageType.Text;

        // فایل پیوست
        public string? AttachmentUrl { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
        public long? AttachmentSize { get; set; }

        // زمان‌ها
        public DateTime SentAt { get; set; } = DateTime.Now; // ⭐ اضافه شد
        public DateTime? DeliveredAt { get; set; }

        // وضعیت‌ها
        public bool IsDelivered { get; set; } = false;
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        // Reply و Forward
        public int? ReplyToMessageId { get; set; }
        public int? ForwardedFromMessageId { get; set; }

        // تماس مرتبط
        public int? CallId { get; set; }

        // پیام سیستمی
        public bool IsSystemMessage { get; set; } = false;

        // Navigation Properties
        public User Sender { get; set; } = null!;
        public User? Receiver { get; set; }
        public Group? Group { get; set; }
        public Channel? Channel { get; set; }
        public Message? ReplyToMessage { get; set; }
        public Message? ForwardedFromMessage { get; set; }
        public Call? Call { get; set; }

        public ICollection<Message> Replies { get; set; } = new List<Message>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<MessageRead> ReadReceipts { get; set; } = new List<MessageRead>();

        public virtual ICollection<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();

    }
}
