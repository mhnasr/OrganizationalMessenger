using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // صفحه اصلی چت
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .Include(u => u.UserGroups)
                    .ThenInclude(ug => ug.Group)
                .Include(u => u.UserChannels)
                    .ThenInclude(uc => uc.Channel)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.CurrentUser = user;
            ViewBag.Chats = await GetUserChats(userId.Value);
            return View();
        }

        // دریافت لیست چتها
        [HttpGet]
        [Route("Chat/GetChats")]  // ← این خط را اضافه کنید

        public async Task<IActionResult> GetChats(string tab = "all")
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var chats = await GetUserChats(userId.Value, tab);
            return Json(chats);
        }

        // دریافت پیامهای یک چت
        [HttpGet]
        public async Task<IActionResult> GetMessages(int? userId = null, int? groupId = null, int? channelId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (userId == null && groupId == null && channelId == null)
                return BadRequest("Destination is not specified.");

            IQueryable<Message> query = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt);

            if (userId.HasValue)
            {
                // چت خصوصی
                query = query.Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId));
            }
            else if (groupId.HasValue)
            {
                // پیام‌های گروه
                query = query.Where(m => m.GroupId == groupId);
            }
            else if (channelId.HasValue)
            {
                // پیام‌های کانال
                query = query.Where(m => m.ChannelId == channelId);
            }

            var messages = await query
                .Select(m => new
                {
                    m.Id,
                    m.MessageText,
                    m.Content,
                    m.Type,
                    m.CreatedAt,
                    m.SentAt,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderAvatar = m.Sender.AvatarUrl,
                    m.GroupId,
                    m.ChannelId,
                    Attachments = m.Attachments.Select(a => new
                    {
                        a.Id,
                        a.FileName,
                        a.FileUrl,
                        a.FileSize,
                        a.FileType
                    }).ToList()
                })
                .ToListAsync();

            return Json(messages);
        }

        // ارسال پیام
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var senderId = GetCurrentUserId();
            if (senderId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.ReceiverId == null && request.GroupId == null && request.ChannelId == null)
                return BadRequest("ReceiverId or GroupId or ChannelId must be specified.");

            var sender = await _context.Users.FindAsync(senderId.Value);
            if (sender == null || !sender.IsActive || sender.IsDeleted)
                return Unauthorized();

            // می‌توان اینجا چک کرد که کاربر عضو گروه/کانال هست یا نه
            if (request.GroupId.HasValue)
            {
                var isMemberOfGroup = await _context.UserGroups
                    .AnyAsync(ug => ug.UserId == senderId.Value && ug.GroupId == request.GroupId && ug.IsActive);
                if (!isMemberOfGroup)
                    return Forbid();
            }

            if (request.ChannelId.HasValue)
            {
                var isMemberOfChannel = await _context.UserChannels
                    .AnyAsync(uc => uc.UserId == senderId.Value && uc.ChannelId == request.ChannelId && uc.IsActive);
                if (!isMemberOfChannel)
                    return Forbid();
            }

            var now = DateTime.Now;

            var message = new Message
            {
                SenderId = senderId.Value,
                ReceiverId = request.ReceiverId,
                GroupId = request.GroupId,
                ChannelId = request.ChannelId,
                MessageText = request.MessageText,
                Content = request.MessageText,
                Type = request.Type,
                SentAt = now,
                CreatedAt = now,
                IsDeleted = false,
               
            };
            try
            {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            }
            catch (Exception e)
            {

                throw;
            }

            return Json(new
            {
                success = true,
                messageId = message.Id,
                sentAt = message.SentAt,
                createdAt = message.CreatedAt
            });
        }

        // متد کمکی برای گرفتن آی‌دی کاربر فعلی
        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
                return null;

            if (int.TryParse(claim.Value, out var id))
                return id;

            return null;
        }

        // متد کمکی
        private async Task<dynamic> GetUserChats(int userId, string tab = "all")
        {
            var user = await _context.Users
                .Include(u => u.UserGroups.Where(ug => ug.IsActive))
                    .ThenInclude(ug => ug.Group)
                        .ThenInclude(g => g.UserGroups)
                .Include(u => u.UserChannels.Where(uc => uc.IsActive))
                    .ThenInclude(uc => uc.Channel)
                        .ThenInclude(c => c.UserChannels)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            var chats = new List<dynamic>();

            // چت‌های خصوصی
            // چت‌های خصوصی - مثل واتساپ (بدون خطا)
            if (tab == "all" || tab == "private")
            {
                var contacts = await _context.Users
                    .Where(u => u.Id != userId && u.IsActive && !u.IsDeleted)
                    .ToListAsync();

                foreach (var contact in contacts)
                {
                    // همه پیام‌های بین من و این مخاطب
                    var lastMessage = await _context.Messages
                        .Where(m => !m.IsDeleted &&
                               ((m.SenderId == userId && m.ReceiverId == contact.Id) ||     // ارسالی
                                (m.SenderId == contact.Id && m.ReceiverId == userId)))      // دریافتی
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    var unreadCount = await _context.Messages
                        .CountAsync(m => m.SenderId == contact.Id &&
                                        m.ReceiverId == userId &&
                                        !m.IsDeleted);

                    // ✅ Safe substring
                    string lastMessageText = "";
                    if (lastMessage?.MessageText != null)
                    {
                        lastMessageText = lastMessage.MessageText.Length > 30
                            ? lastMessage.MessageText.Substring(0, 30)
                            : lastMessage.MessageText;
                    }
                    else if (lastMessage?.Content != null)
                    {
                        lastMessageText = lastMessage.Content.Length > 30
                            ? lastMessage.Content.Substring(0, 30)
                            : lastMessage.Content;
                    }

                    chats.Add(new
                    {
                        type = "private",
                        id = contact.Id,
                        name = contact.FullName ?? "کاربر",
                        avatar = contact.AvatarUrl ?? "/images/default-avatar.png",
                        isOnline = contact.IsOnline,
                        lastMessage = lastMessageText + (lastMessageText.Length > 29 ? "..." : ""),
                        lastMessageTime = lastMessage?.CreatedAt ?? contact.CreatedAt,
                        unreadCount = unreadCount,
                        messageDirection = lastMessage?.SenderId == userId ? "sent" : "received"
                    });
                }
            }

            // ✅ سورت - جدیدترین بالا (واتساپ)
            


            // گروه‌ها
            if (tab == "all" || tab == "group")
            {
                foreach (var ug in user.UserGroups)
                {
                    var lastMessage = await _context.Messages
                        .Where(m => m.GroupId == ug.GroupId && !m.IsDeleted)
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    chats.Add(new
                    {
                        type = "group",
                        id = ug.GroupId,
                        name = ug.Group.Name,
                        lastMessage = lastMessage?.Content ?? "بدون پیام",
                        lastMessageTime = lastMessage?.CreatedAt ?? ug.Group.CreatedAt,
                        memberCount = ug.Group.UserGroups.Count
                    });
                }
            }

            // کانال‌ها
            if (tab == "all" || tab == "channel")
            {
                foreach (var uc in user.UserChannels)
                {
                    var lastMessage = await _context.Messages
                        .Where(m => m.ChannelId == uc.ChannelId && !m.IsDeleted)
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    chats.Add(new
                    {
                        type = "channel",
                        id = uc.ChannelId,
                        name = uc.Channel.Name,
                        lastMessage = lastMessage?.Content ?? "بدون پیام",
                        lastMessageTime = lastMessage?.CreatedAt ?? uc.Channel.CreatedAt,
                        memberCount = uc.Channel.UserChannels.Count
                    });
                }
            }

            return chats
                .OrderByDescending(c => c.lastMessageTime)
                .ToList();
        }
       
        
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // پاک کردن تمام cookie ها
            Response.Cookies.Delete(".AspNetCore.Cookies");

            return RedirectToAction("Login", "Account");
        }

    }

    public class SendMessageRequest
    {
        public int? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public int? ChannelId { get; set; }
        public string? MessageText { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
    }
}
