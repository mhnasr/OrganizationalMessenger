namespace OrganizationalMessenger.Domain.Entities
{
    public class PollOption
    {
        public int Id { get; set; }
        public int PollId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        // Navigation Properties
        public Poll Poll { get; set; } = null!;
        public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
    }
}
