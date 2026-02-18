using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using System.Security.Claims;

namespace OrganizationalMessenger.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public GroupController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            return int.TryParse(claim.Value, out var id) ? id : null;
        }


        [HttpGet("CanCreateGroup")]
        public IActionResult CanCreateGroup()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, canCreate = false });

            try
            {
                var user = _context.Users.Find(userId.Value);
                return Ok(new
                {
                    success = true,
                    canCreate = user?.CanCreateGroup ?? false
                });
            }
            catch
            {
                return Ok(new { success = true, canCreate = false });
            }
        }





        // ✅ تغییر [FromBody] به [FromForm]
        [HttpPost("Create")]
        public async Task<IActionResult> CreateGroup([FromForm] CreateGroupRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, message = "کاربر احراز هویت نشده" });

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null || !user.CanCreateGroup)
                {
                    return StatusCode(403, new { success = false, message = "شما مجوز ایجاد گروه ندارید" });
                }

                string? avatarUrl = null;

                // ✅ پردازش فایل آواتار
                if (request.AvatarFile != null && request.AvatarFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "groups");
                    Directory.CreateDirectory(uploadsPath);

                    var ext = Path.GetExtension(request.AvatarFile.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await request.AvatarFile.CopyToAsync(stream);
                    }

                    avatarUrl = $"/uploads/groups/{fileName}";
                }

                var group = new Group
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatorId = userId.Value,
                    IsPublic = request.IsPublic,
                    MaxMembers = request.MaxMembers ?? 200,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    AvatarUrl = avatarUrl ?? "/images/default-group.png"
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

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

        // ✅ DTO با IFormFile
        public class CreateGroupRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsPublic { get; set; } = false;
            public int? MaxMembers { get; set; }
            public IFormFile? AvatarFile { get; set; } // ✅ فایل
        }

        // ✅ بقیه متدها (GetMyGroups و...) همان‌طور که قبلاً بود
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
                        id = ug.Group.Id,
                        name = ug.Group.Name,
                        description = ug.Group.Description,
                        avatar = ug.Group.AvatarUrl ?? "/images/default-group.png",
                        isPublic = ug.Group.IsPublic,
                        memberCount = _context.UserGroups.Count(x => x.GroupId == ug.GroupId && x.IsActive),
                        role = ug.Role.ToString(),
                        isAdmin = ug.IsAdmin,
                        isMuted = ug.IsMuted,
                        lastMessage = _context.Messages
                            .Where(m => m.GroupId == ug.GroupId && !m.IsDeleted)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.Content ?? m.MessageText)
                            .FirstOrDefault(),
                        lastMessageTime = _context.Messages
                            .Where(m => m.GroupId == ug.GroupId && !m.IsDeleted)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.SentAt)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(g => g.lastMessageTime)
                    .ToListAsync();

                return Ok(new { success = true, groups });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        // ✅ دریافت لیست اعضای گروه
        // ✅ دریافت لیست اعضای گروه
        [HttpGet("{groupId}/Members")]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var userGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsActive);

                if (userGroup == null)
                    return NotFound(new { success = false, message = "شما عضو این گروه نیستید" });

                // ✅ ابتدا داده‌ها را از دیتابیس بگیر، بعد projection سمت کلاینت
                var membersRaw = await _context.UserGroups
                    .Where(ug => ug.GroupId == groupId && ug.IsActive)
                    .Include(ug => ug.User)
                    .OrderByDescending(ug => ug.IsAdmin)
                    .ToListAsync();

                var members = membersRaw
                    .Select(ug => new
                    {
                        userId = ug.UserId,
                        name = $"{ug.User.FirstName} {ug.User.LastName}",
                        username = ug.User.Username,
                        avatar = ug.User.AvatarUrl ?? "/images/default-avatar.png",
                        role = ug.Role.ToString(),
                        isAdmin = ug.IsAdmin,
                        joinedAt = ug.JoinedAt,
                        isOnline = ug.User.IsOnline
                    })
                    .OrderByDescending(m => m.isAdmin)
                    .ThenBy(m => m.name)
                    .ToList();

                return Ok(new { success = true, members });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ جستجوی کاربران برای اضافه کردن
        // ✅ جستجوی کاربران برای اضافه کردن
        [HttpGet("{groupId}/SearchUsers")]
        public async Task<IActionResult> SearchUsersForGroup(int groupId, [FromQuery] string query = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var adminGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsAdmin);

                if (adminGroup == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز اضافه کردن عضو ندارید" });

                var existingMemberIds = await _context.UserGroups
                    .Where(ug => ug.GroupId == groupId && ug.IsActive)
                    .Select(ug => ug.UserId)
                    .ToListAsync();

                // ✅ ابتدا فیلتر در دیتابیس، بعد projection سمت کلاینت
                var usersRaw = await _context.Users
                    .Where(u => u.IsActive &&
                               !u.IsDeleted &&
                               !existingMemberIds.Contains(u.Id) &&
                               (string.IsNullOrEmpty(query) ||
                                u.FirstName.Contains(query) ||
                                u.LastName.Contains(query) ||
                                u.Username.Contains(query)))
                    .Take(20)
                    .ToListAsync();

                var users = usersRaw.Select(u => new
                {
                    id = u.Id,
                    name = $"{u.FirstName} {u.LastName}",
                    username = u.Username,
                    avatar = u.AvatarUrl ?? "/images/default-avatar.png",
                    isOnline = u.IsOnline
                }).ToList();

                return Ok(new { success = true, users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ اضافه کردن عضو به گروه
        [HttpPost("{groupId}/AddMember")]
        public async Task<IActionResult> AddMemberToGroup(int groupId, [FromBody] AddMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // بررسی مجوز (باید ادمین باشد)
                var adminGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsAdmin);

                if (adminGroup == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز اضافه کردن عضو ندارید" });

                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                    return NotFound(new { success = false, message = "گروه یافت نشد" });

                // بررسی حداکثر اعضا
                var currentMemberCount = await _context.UserGroups
                    .CountAsync(ug => ug.GroupId == groupId && ug.IsActive);

                if (currentMemberCount >= group.MaxMembers)
                    return BadRequest(new { success = false, message = $"حداکثر تعداد اعضا ({group.MaxMembers}) محدود شده است" });

                // بررسی عضویت قبلی
                var existing = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == request.UserId);

                if (existing != null && existing.IsActive)
                    return BadRequest(new { success = false, message = "کاربر قبلاً عضو است" });

                if (existing != null && !existing.IsActive)
                {
                    // فعال‌سازی مجدد
                    existing.IsActive = true;
                    existing.JoinedAt = DateTime.Now;
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
                }

                await _context.SaveChangesAsync();

                var addedUser = await _context.Users.FindAsync(request.UserId);

                return Ok(new
                {
                    success = true,
                    message = "عضو با موفقیت اضافه شد",
                    member = new
                    {
                        userId = addedUser.Id,
                        name = $"{addedUser.FirstName} {addedUser.LastName}",
                        username = addedUser.Username,
                        avatar = addedUser.AvatarUrl ?? "/images/default-avatar.png",
                        role = "Member",
                        isAdmin = false
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ حذف عضو از گروه
        [HttpPost("{groupId}/RemoveMember")]
        public async Task<IActionResult> RemoveMemberFromGroup(int groupId, [FromBody] RemoveMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var adminGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value && ug.IsAdmin);

                if (adminGroup == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز حذف عضو ندارید" });

                var userGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == request.UserId);

                if (userGroup == null)
                    return NotFound(new { success = false, message = "کاربر عضو این گروه نیست" });

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

        // ✅ DTOs
        public class AddMemberRequest
        {
            public int UserId { get; set; }
        }

        public class RemoveMemberRequest
        {
            public int UserId { get; set; }
        }


    }
}