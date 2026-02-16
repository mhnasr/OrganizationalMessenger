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
    public class Channel1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public Channel1Controller(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        // ✅ ایجاد کانال جدید
        [HttpPost("Create")]
        public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // ✅ بررسی مجوز
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null || !user.CanCreateChannel)
                {
                    return StatusCode(403, new { success = false, message = "شما مجوز ایجاد کانال ندارید" });
                }

                // ✅ ایجاد کانال
                var channel = new Channel
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatorId = userId.Value,
                    IsPublic = request.IsPublic,
                    OnlyAdminsCanPost = request.OnlyAdminsCanPost,
                    AllowComments = request.AllowComments,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                // ✅ تولید لینک دعوت
                if (!channel.IsPublic)
                {
                    channel.GenerateInviteLink();
                }

                _context.Channels.Add(channel);
                await _context.SaveChangesAsync();

                // ✅ اضافه کردن سازنده به عنوان Owner
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

                // ✅ به‌روزرسانی تعداد اعضا
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

        // ✅ دریافت لیست کانال‌های کاربر
        [HttpGet("MyChannels")]
        public async Task<IActionResult> GetMyChannels()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var channels = await _context.UserChannels
                    .Where(uc => uc.UserId == userId.Value && uc.IsActive)
                    .Include(uc => uc.Channel)
                        .ThenInclude(c => c.Creator)
                    .Select(uc => new
                    {
                        uc.Channel.Id,
                        uc.Channel.Name,
                        uc.Channel.Description,
                        Avatar = uc.Channel.AvatarUrl ?? "/images/default-channel.png",
                        uc.Channel.IsPublic,
                        uc.Channel.MemberCount,
                        Role = uc.Role.ToString(),
                        uc.IsAdmin,
                        uc.CanPost,
                        uc.IsMuted,
                        uc.IsPinned,
                        UnreadCount = uc.UnreadCount,
                        LastMessage = _context.Messages
                            .Where(m => m.ChannelId == uc.ChannelId && !m.IsDeleted)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.Content ?? m.MessageText)
                            .FirstOrDefault(),
                        LastMessageTime = _context.Messages
                            .Where(m => m.ChannelId == uc.ChannelId && !m.IsDeleted)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.SentAt)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(c => c.IsPinned)
                    .ThenByDescending(c => c.LastMessageTime)
                    .ToListAsync();

                return Ok(new { success = true, channels });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ دریافت اطلاعات یک کانال
        [HttpGet("{channelId}")]
        public async Task<IActionResult> GetChannel(int channelId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var userChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId.Value && uc.IsActive);

                if (userChannel == null)
                    return NotFound(new { success = false, message = "شما عضو این کانال نیستید" });

                var channel = await _context.Channels
                    .Include(c => c.Creator)
                    .FirstOrDefaultAsync(c => c.Id == channelId && !c.IsDeleted);

                if (channel == null)
                    return NotFound(new { success = false, message = "کانال یافت نشد" });

                var admins = await _context.UserChannels
                    .Where(uc => uc.ChannelId == channelId && uc.IsActive && uc.IsAdmin)
                    .Include(uc => uc.User)
                    .Select(uc => new
                    {
                        uc.User.Id,
                        Name = $"{uc.User.FirstName} {uc.User.LastName}",
                        Avatar = uc.User.AvatarUrl ?? "/images/default-avatar.png",
                        Role = uc.Role.ToString(),
                        uc.CanPost,
                        uc.JoinedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    channel = new
                    {
                        channel.Id,
                        channel.Name,
                        channel.Description,
                        Avatar = channel.AvatarUrl ?? "/images/default-channel.png",
                        channel.IsPublic,
                        channel.OnlyAdminsCanPost,
                        channel.AllowComments,
                        channel.MemberCount,
                        Creator = $"{channel.Creator.FirstName} {channel.Creator.LastName}",
                        channel.CreatedAt,
                        MyRole = userChannel.Role.ToString(),
                        IsAdmin = userChannel.IsAdmin,
                        CanPost = userChannel.CanPost,
                        IsMuted = userChannel.IsMuted,
                        channel.InviteLink
                    },
                    admins
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ اضافه کردن عضو به کانال
        [HttpPost("{channelId}/AddMember")]
        public async Task<IActionResult> AddMember(int channelId, [FromBody] AddMemberRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // ✅ بررسی مجوز (باید ادمین باشد)
                var adminChannel = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId.Value && uc.IsAdmin);

                if (adminChannel == null)
                    return StatusCode(403, new { success = false, message = "شما مجوز اضافه کردن عضو ندارید" });

                // ✅ بررسی وجود قبلی
                var existing = await _context.UserChannels
                    .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == request.UserId);

                if (existing != null)
                {
                    if (existing.IsActive)
                        return BadRequest(new { success = false, message = "کاربر قبلاً عضو است" });

                    // ✅ فعال‌سازی مجدد
                    existing.Rejoin();
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // ✅ اضافه کردن عضو جدید
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

                // ✅ به‌روزرسانی تعداد اعضا
                var channel = await _context.Channels.FindAsync(channelId);
                if (channel != null)
                {
                    channel.MemberCount = await _context.UserChannels
                        .CountAsync(uc => uc.ChannelId == channelId && uc.IsActive);
                    channel.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "عضو با موفقیت اضافه شد" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ جستجوی کانال‌های عمومی
        [HttpGet("Search")]
        public async Task<IActionResult> SearchPublicChannels([FromQuery] string query)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var channels = await _context.Channels
                    .Where(c => c.IsPublic && !c.IsDeleted && c.IsActive &&
                                c.Name.Contains(query))
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        Avatar = c.AvatarUrl ?? "/images/default-channel.png",
                        c.MemberCount,
                        IsJoined = _context.UserChannels
                            .Any(uc => uc.ChannelId == c.Id && uc.UserId == userId.Value && uc.IsActive)
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(new { success = true, channels });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    // ✅ DTOs
    public class CreateChannelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool OnlyAdminsCanPost { get; set; } = true;
        public bool AllowComments { get; set; } = false;
    }

    public class AddMemberRequest
    {
        public int UserId { get; set; }
    }
}