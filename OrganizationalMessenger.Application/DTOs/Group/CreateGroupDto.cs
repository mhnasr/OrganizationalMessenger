// Path: OrganizationalMessenger.Application/DTOs/Group/CreateGroupDto.cs

namespace OrganizationalMessenger.Application.DTOs.Group
{
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public int? MaxMembers { get; set; } = 200;

        public string? AvatarUrl { get; set; }  // لینک عکس ذخیره شده
    }
}