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
    public class ChannelController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ChannelController(ApplicationDbContext context, IWebHostEnvironment env)
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

        // ✅ تغییر [FromBody] به [FromForm]
        [HttpPost("Create")]
        public async Task<IActionResult> CreateChannel([FromForm] CreateChannelRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, message = "کاربر احراز هویت نشده" });

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null || !user.CanCreateChannel)
                {
                    return StatusCode(403, new { success = false, message = "شما مجوز ایجاد کانال ندارید" });
                }

                string? avatarUrl = null;

                // ✅ پردازش فایل آواتار
                if (request.AvatarFile != null && request.AvatarFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "channels");
                    Directory.CreateDirectory(uploadsPath);

                    var ext = Path.GetExtension(request.AvatarFile.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await request.AvatarFile.CopyToAsync(stream);
                    }

                    avatarUrl = $"/uploads/channels/{fileName}";
                }

                var channel = new Channel
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatorId = userId.Value,
                    IsPublic = request.IsPublic,
                    OnlyAdminsCanPost = request.OnlyAdminsCanPost,
                    AllowComments = request.AllowComments,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    AvatarUrl = avatarUrl ?? "/images/default-channel.png"
                };

                _context.Channels.Add(channel);
                await _context.SaveChangesAsync();

                var userChannel = new UserChannel
                {
                    UserId = userId.Value,
                    ChannelId = channel.Id,
                    Role = ChannelRole.Owner,
                    IsOwner = true,
                    IsAdmin = true,
                    CanPost = true,
                    CanDeleteMessages = true,
                    CanManageMembers = true,
                    JoinedAt = DateTime.Now,
                    IsActive = true
                };

                _context.UserChannels.Add(userChannel);
                channel.MemberCount = 1;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "کانال با موفقیت ایجاد شد",
                    channelId = channel.Id,
                    channel = new
                    {
                        channel.Id,
                        channel.Name,
                        channel.Description,
                        channel.AvatarUrl,
                        channel.IsPublic,
                        channel.OnlyAdminsCanPost,
                        channel.AllowComments,
                        channel.InviteLink,
                        channel.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ DTO
        public class CreateChannelRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsPublic { get; set; } = true;
            public bool OnlyAdminsCanPost { get; set; } = true;
            public bool AllowComments { get; set; } = false;
            public IFormFile? AvatarFile { get; set; }
        }

        // ✅ دریافت لیست اعضای کانال
        [HttpGet("{channelId}/Members")]
        public async Task<IActionResult> GetChannelMembers(int channelId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var userChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId.Value && uc.IsActive);

                if (userChannel == null)
                    return NotFound(new { success = false, message = "شما عضو این کانال نیستید" });

                var members = await _context.UserChannels
                    .Where(uc => uc.ChannelId == channelId && uc.IsActive)
                    .Include(uc => uc.User)
                    .Select(uc => new
                    {
                        userId = uc.UserId,
                        name = $"{uc.User.FirstName} {uc.User.LastName}",
                        username = uc.User.Username,
                        avatar = uc.User.AvatarUrl ?? "/images/default-avatar.png",
                        role = uc.Role.ToString(),
                        isAdmin = uc.IsAdmin,
                        canPost = uc.CanPost,
                        joinedAt = uc.JoinedAt,
                        isOnline = uc.User.IsOnline
                    })
                    .OrderByDescending(m => m.isAdmin)
                    .ThenBy(m => m.name)
                    .ToListAsync();

                return Ok(new { success = true, members });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ جستجوی کاربران برای اضافه کردن
        [HttpGet("{channelId}/SearchUsers")]
        public async Task<IActionResult> SearchUsersForChannel(int channelId, [FromQuery] string query = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var adminChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId.Value && uc.IsAdmin);

                if (adminChannel == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز اضافه کردن عضو ندارید" });

                var existingMemberIds = await _context.UserChannels
                    .Where(uc => uc.ChannelId == channelId && uc.IsActive)
                    .Select(uc => uc.UserId)
                    .ToListAsync();

                var users = await _context.Users
                    .Where(u => u.IsActive &&
                               !u.IsDeleted &&
                               !existingMemberIds.Contains(u.Id) &&
                               (string.IsNullOrEmpty(query) ||
                                u.FirstName.Contains(query) ||
                                u.LastName.Contains(query) ||
                                u.Username.Contains(query)))
                    .Select(u => new
                    {
                        id = u.Id,
                        name = $"{u.FirstName} {u.LastName}",
                        username = u.Username,
                        avatar = u.AvatarUrl ?? "/images/default-avatar.png",
                        isOnline = u.IsOnline
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(new { success = true, users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ اضافه کردن عضو به کانال
        [HttpPost("{channelId}/AddMember")]
        public async Task<IActionResult> AddMemberToChannel(int channelId, [FromBody] AddChannelMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var adminChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId.Value && uc.IsAdmin);

                if (adminChannel == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز اضافه کردن عضو ندارید" });

                var existing = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == request.UserId);

                if (existing != null && existing.IsActive)
                    return BadRequest(new { success = false, message = "کاربر قبلاً عضو است" });

                if (existing != null && !existing.IsActive)
                {
                    existing.Rejoin();
                }
                else
                {
                    var userChannel = new UserChannel
                    {
                        UserId = request.UserId,
                        ChannelId = channelId,
                        Role = ChannelRole.Subscriber,
                        IsActive = true,
                        JoinedAt = DateTime.Now
                    };

                    _context.UserChannels.Add(userChannel);
                }

                var channel = await _context.Channels.FindAsync(channelId);
                if (channel != null)
                {
                    channel.MemberCount = await _context.UserChannels
                        .CountAsync(uc => uc.ChannelId == channelId && uc.IsActive);
                    channel.UpdatedAt = DateTime.Now;
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
                        role = "Subscriber",
                        isAdmin = false,
                        canPost = false
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ حذف عضو از کانال
        [HttpPost("{channelId}/RemoveMember")]
        public async Task<IActionResult> RemoveMemberFromChannel(int channelId, [FromBody] RemoveChannelMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var adminChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId.Value && uc.IsAdmin);

                if (adminChannel == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز حذف عضو ندارید" });

                var userChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == request.UserId);

                if (userChannel == null)
                    return NotFound(new { success = false, message = "کاربر عضو این کانال نیست" });

                if (userChannel.IsOwner)
                    return BadRequest(new { success = false, message = "نمی‌توان سازنده کانال را حذف کرد" });

                userChannel.Leave();

                var channel = await _context.Channels.FindAsync(channelId);
                if (channel != null)
                {
                    channel.MemberCount = await _context.UserChannels
                        .CountAsync(uc => uc.ChannelId == channelId && uc.IsActive);
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "عضو حذف شد" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ DTOs
        public class AddChannelMemberRequest
        {
            public int UserId { get; set; }
        }

        public class RemoveChannelMemberRequest
        {
            public int UserId { get; set; }
        }
    }
}