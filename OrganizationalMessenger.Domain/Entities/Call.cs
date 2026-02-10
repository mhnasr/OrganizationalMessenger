using OrganizationalMessenger.Domain.Enums;

namespace OrganizationalMessenger.Domain.Entities
{
    public class Call : BaseEntity
    {
        public int InitiatorId { get; set; }
        public int? ReceiverId { get; set; }
        public CallType Type { get; set; }
        public CallDestinationType DestinationType { get; set; }

        // مقصد تماس
        public int? GroupId { get; set; }
        public int? ChannelId { get; set; }

        // وضعیت
        public CallStatus Status { get; set; } = CallStatus.Initiated;
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public int? Duration { get; set; }

        // لینک جلسه گروهی
        public string? MeetingLink { get; set; }
        public string? MeetingPassword { get; set; }

        // Navigation Properties
        public User Initiator { get; set; } = null!;
        public User? Receiver { get; set; }
        public Group? Group { get; set; }
        public Channel? Channel { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
