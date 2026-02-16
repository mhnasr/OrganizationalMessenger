namespace OrganizationalMessenger.Application.DTOs.Channel
{
    public class CreateChannelDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool OnlyAdminsCanPost { get; set; } = true;
        public bool AllowComments { get; set; } = false;
        public string? AvatarUrl { get; set; }  // لینک عکس ذخیره شده
    }
}