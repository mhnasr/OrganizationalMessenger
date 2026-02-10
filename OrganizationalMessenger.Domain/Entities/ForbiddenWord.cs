namespace OrganizationalMessenger.Domain.Entities
{
    public class ForbiddenWord
    {
        public int Id { get; set; }
        public string Word { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
