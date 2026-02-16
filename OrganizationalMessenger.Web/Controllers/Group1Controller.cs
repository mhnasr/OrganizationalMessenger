using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;

namespace OrganizationalMessenger.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Group1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public Group1Controller(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        // ✅ ایجاد گروه جدید
        [HttpPost("Create")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // ✅ بررسی مجوز
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null || !user.CanCreateGroup)
                {
                    return Forbid("شما مجوز ایجاد گروه ندارید");
                }

                // ✅ ایجاد گروه
                var group = new Group
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatorId = userId.Value,
                    IsPublic = request.IsPublic,
                    MaxMembers = request.MaxMembers ?? 200,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                // ✅ اضافه کردن سازنده به عنوان Owner
                var userGroup = new UserGroup
                {
                    UserId = userId.Value,
                    GroupId = group.Id,
                    Role = GroupRole.Owner,
                    IsAdmin = true,
                    JoinedAt = DateTime.Now,
                    IsActive = true
                };

                _context.UserGroups.Add(userGroup);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "گروه با موفقیت ایجاد شد",
                    groupId = group.Id,
                    group = new
                    {
                        group.Id,
                        group.Name,
                        group.Description,
                        group.AvatarUrl,
                        group.IsPublic,
                        group.MaxMembers,
                        group.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ دریافت لیست گروه‌های کاربر
        [HttpGet("MyGroups")]
        public async Task<IActionResult> GetMyGroups()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var groups = await _context.UserGroups
                    .Where(ug => ug.UserId == userId.Value && ug.IsActive)
                    .Include(ug => ug.Group)
                        .ThenInclude(g => g.Creator)
                    .Select(ug => new
                    {
                        ug.Group.Id,
                        ug.Group.Name,
                        ug.Group.Description,
                        Avatar = ug.Group.AvatarUrl ?? "/images/default-group.png",
                        ug.Group.IsPublic,
                        MemberCount = _context.UserGroups.Count(x => x.GroupId == ug.GroupId && x.IsActive),
                        Role = ug.Role.ToString(),
                        ug.IsAdmin,
                        ug.IsMuted,
                        UnreadCount = 0, // TODO: محاسبه پیام‌های خوانده نشده
                        LastMessage = _context.Messages
                            .Where(m => m.GroupId == ug.GroupId && !m.IsDeleted)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.Content ?? m.MessageText)
                            .FirstOrDefault(),
                        LastMessageTime = _context.Messages
                            .Where(m => m.GroupId == ug.GroupId && !m.IsDeleted)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.SentAt)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(g => g.LastMessageTime)
                    .ToListAsync();

                return Ok(new { success = true, groups });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ دریافت اطلاعات یک گروه
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroup(int groupId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var userGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsActive);

                if (userGroup == null)
                    return NotFound(new { success = false, message = "شما عضو این گروه نیستید" });

                var group = await _context.Groups
                    .Include(g => g.Creator)
                    .FirstOrDefaultAsync(g => g.Id == groupId);

                if (group == null)
                    return NotFound(new { success = false, message = "گروه یافت نشد" });

                var members = await _context.UserGroups
                    .Where(ug => ug.GroupId == groupId && ug.IsActive)
                    .Include(ug => ug.User)
                    .Select(ug => new
                    {
                        ug.User.Id,
                        Name = $"{ug.User.FirstName} {ug.User.LastName}",
                        Avatar = ug.User.AvatarUrl ?? "/images/default-avatar.png",
                        Role = ug.Role.ToString(),
                        ug.IsAdmin,
                        ug.JoinedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    group = new
                    {
                        group.Id,
                        group.Name,
                        group.Description,
                        Avatar = group.AvatarUrl ?? "/images/default-group.png",
                        group.IsPublic,
                        group.MaxMembers,
                        Creator = $"{group.Creator.FirstName} {group.Creator.LastName}",
                        group.CreatedAt,
                        MemberCount = members.Count,
                        MyRole = userGroup.Role.ToString(),
                        IsAdmin = userGroup.IsAdmin,
                        IsMuted = userGroup.IsMuted
                    },
                    members
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        // ✅ اضافه کردن عضو به گروه
        [HttpPost("{groupId}/AddMember")]
        public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // ✅ بررسی مجوز (باید ادمین باشد)
                var adminGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsAdmin);

                if (adminGroup == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز اضافه کردن عضو ندارید" });

                // ✅ بررسی حداکثر اعضا
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                    return NotFound(new { success = false, message = "گروه یافت نشد" });

                var currentMemberCount = await _context.UserGroups
                    .CountAsync(ug => ug.GroupId == groupId && ug.IsActive);

                if (currentMemberCount >= group.MaxMembers)
                    return BadRequest(new { success = false, message = $"حداکثر تعداد اعضا ({group.MaxMembers}) محدود شده است" });

                // ✅ بررسی وجود قبلی
                var existing = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == request.UserId);

                if (existing != null)
                {
                    if (existing.IsActive)
                        return BadRequest(new { success = false, message = "کاربر قبلاً عضو است" });

                    // فعال‌سازی مجدد
                    existing.IsActive = true;
                    existing.JoinedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // اضافه کردن عضو جدید
                    var userGroup = new UserGroup
                    {
                        UserId = request.UserId,
                        GroupId = groupId,
                        Role = GroupRole.Member,
                        IsActive = true,
                        JoinedAt = DateTime.Now
                    };

                    _context.UserGroups.Add(userGroup);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "عضو با موفقیت اضافه شد" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ حذف عضو از گروه
        [HttpPost("{groupId}/RemoveMember")]
        public async Task<IActionResult> RemoveMember(int groupId, [FromBody] RemoveMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // ✅ بررسی مجوز
                var adminGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsAdmin);

                if (adminGroup == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز حذف عضو ندارید" });

                var userGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == request.UserId);

                if (userGroup == null)
                    return NotFound(new { success = false, message = "کاربر عضو این گروه نیست" });

                // ✅ نمی‌توان سازنده را حذف کرد
                if (userGroup.Role == GroupRole.Owner)
                    return BadRequest(new { success = false, message = "نمی‌توان سازنده گروه را حذف کرد" });

                userGroup.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "عضو حذف شد" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ تغییر نقش عضو
        [HttpPost("{groupId}/ChangeRole")]
        public async Task<IActionResult> ChangeRole(int groupId, [FromBody] ChangeRoleRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var adminGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.Role == GroupRole.Owner);

                if (adminGroup == null)
                    return StatusCode(403, new { success = false, message = "فقط سازنده می‌تواند نقش‌ها را تغییر دهد" });

                var userGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == request.UserId);

                if (userGroup == null)
                    return NotFound(new { success = false, message = "کاربر یافت نشد" });

                userGroup.Role = request.NewRole;
                userGroup.IsAdmin = request.NewRole == GroupRole.Admin || request.NewRole == GroupRole.Owner;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "نقش تغییر یافت" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ ترک گروه
        [HttpPost("{groupId}/Leave")]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var userGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value);

                if (userGroup == null)
                    return NotFound(new { success = false, message = "شما عضو این گروه نیستید" });

                if (userGroup.Role == GroupRole.Owner)
                    return BadRequest(new { success = false, message = "سازنده نمی‌تواند گروه را ترک کند" });

                userGroup.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "گروه را ترک کردید" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ DTOs
        public class AddMemberRequest
        {
            public int UserId { get; set; }
        }

        public class RemoveMemberRequest
        {
            public int UserId { get; set; }
        }

        public class ChangeRoleRequest
        {
            public int UserId { get; set; }
            public GroupRole NewRole { get; set; }
        }
    }

    // ✅ DTOs
    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public int? MaxMembers { get; set; }
    }
}