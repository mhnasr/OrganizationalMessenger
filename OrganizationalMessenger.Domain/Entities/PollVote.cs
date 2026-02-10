namespace OrganizationalMessenger.Domain.Entities
{
    public class PollVote
    {
        public int Id { get; set; }
        public int PollOptionId { get; set; }
        public int UserId { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public PollOption PollOption { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
