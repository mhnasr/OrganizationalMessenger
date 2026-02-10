using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;

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
            var userId = int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.UserGroups)
                .Include(u => u.UserChannels)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.CurrentUser = user;
            ViewBag.Chats = await GetUserChats(userId);
            return View();
        }

        // دریافت لیست چتها
        [HttpGet]
        public async Task<IActionResult> GetChats(string tab = "all")
        {
            var userId = int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");
            var chats = await GetUserChats(userId, tab);
            return Json(chats);
        }

        // دریافت پیامهای یک چت
        [HttpGet]
        public async Task<IActionResult> GetMessages(int? userId = null, int? groupId = null, int? channelId = null)
        {
            var currentUserId = int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");

            IQueryable<Message> query = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt);

            if (userId.HasValue)
                query = query.Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                        (m.SenderId == userId && m.ReceiverId == currentUserId));
            else if (groupId.HasValue)
                query = query.Where(m => m.GroupId == groupId);
            else if (channelId.HasValue)
                query = query.Where(m => m.ChannelId == channelId);

            var messages = await query.ToListAsync();
            return Json(messages);
        }

        // ارسال پیام
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var senderId = int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");
            var sender = await _context.Users.FindAsync(senderId);

            if (sender == null)
                return Unauthorized();

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                GroupId = request.GroupId,
                ChannelId = request.ChannelId,
                MessageText = request.MessageText,
                Content = request.MessageText,
                Type = request.Type,
                SentAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.Id });
        }

        // متد کمکی
        private async Task<dynamic> GetUserChats(int userId, string tab = "all")
        {
            var user = await _context.Users
                .Include(u => u.SentMessages.Where(m => !m.IsDeleted))
                .Include(u => u.ReceivedMessages.Where(m => !m.IsDeleted))
                .Include(u => u.UserGroups.Where(ug => ug.IsActive))
                    .ThenInclude(ug => ug.Group)
                .Include(u => u.UserChannels.Where(uc => uc.IsActive))
                    .ThenInclude(uc => uc.Channel)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            var chats = new List<dynamic>();

            // چتهای خصوصی
            if (tab == "all" || tab == "private")
            {
                var contacts = await _context.Users
                    .Where(u => u.Id != userId && u.IsActive && !u.IsDeleted)
                    .ToListAsync();

                foreach (var contact in contacts)
                {
                    var lastMessage = await _context.Messages
                        .Where(m => !m.IsDeleted &&
                               ((m.SenderId == userId && m.ReceiverId == contact.Id) ||
                                (m.SenderId == contact.Id && m.ReceiverId == userId)))
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (lastMessage != null)
                    {
                        chats.Add(new
                        {
                            type = "private",
                            id = contact.Id,
                            name = contact.FullName,
                            avatar = contact.AvatarUrl,
                            isOnline = contact.IsOnline,
                            lastMessage = lastMessage.MessageText ?? lastMessage.Content,
                            lastMessageTime = lastMessage.CreatedAt,
                            unreadCount = await _context.Messages
                                .CountAsync(m => m.SenderId == contact.Id &&
                                           m.ReceiverId == userId &&
                                           !m.IsDeleted)
                        });
                    }
                }
            }

            // گروهها
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

            // کانالها
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

            return chats.OrderByDescending(c => c.lastMessageTime).ToList();
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