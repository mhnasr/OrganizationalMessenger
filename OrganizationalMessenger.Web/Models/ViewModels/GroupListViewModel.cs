namespace OrganizationalMessenger.Web.Models.ViewModels
{
    /// <summary>
    /// ViewModel برای نمایش لیست گروه‌ها در پنل ادمین
    /// </summary>
    public class GroupListViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ProfilePicture { get; set; }

        public int CreatorId { get; set; }

        public string CreatorName { get; set; } = string.Empty;

        public string? CreatorUsername { get; set; }

        public int MemberCount { get; set; }

        public int AdminCount { get; set; }

        public int MessageCount { get; set; }

        public int MaxMembers { get; set; }

        public bool IsActive { get; set; }

        public bool IsPublic { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public DateTime? LastActivityAt { get; set; }
    }
}
