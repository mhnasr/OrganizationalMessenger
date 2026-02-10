using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;

namespace OrganizationalMessenger.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        // فهرست کاربران آنلاین: userId -> connectionId
        private static Dictionary<int, string> OnlineUsers = new();

        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// اتصال کاربر
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int id))
            {
                OnlineUsers[id] = Context.ConnectionId;

                // به‌روزرسانی وضعیت کاربر
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastSeen = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // ارسال وضعیت آنلاین به همه کاربران
                    await Clients.All.SendAsync("UserOnline", id, user.FirstName, user.LastName);
                }

                _logger.LogInformation($"کاربر {id} متصل شد");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// قطع اتصال کاربر
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int id))
            {
                OnlineUsers.Remove(id);

                // به‌روزرسانی وضعیت کاربر
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // ارسال وضعیت آفلاین به همه کاربران
                    await Clients.All.SendAsync("UserOffline", id, DateTime.Now);
                }

                _logger.LogInformation($"کاربر {id} قطع اتصال شد");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// ارسال پیام خصوصی
        /// </summary>
        // در متد SendPrivateMessage
        public async Task SendPrivateMessage(int receiverId, string content)
        {
            var senderIdStr = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (!int.TryParse(senderIdStr, out int senderId))
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            try
            {
                // ✅ 1. ذخیره در دیتابیس
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    MessageText = content,
                    Type = MessageType.Text,
                    SentAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync(); // ✅ ذخیره میشه!

                Console.WriteLine($"✅ Message saved: ID={message.Id}, From={senderId} To={receiverId}");

                // 2. اطلاعات فرستنده
                var sender = await _context.Users.FindAsync(senderId);

                // 3. ارسال به گیرنده
                if (OnlineUsers.TryGetValue(receiverId, out var receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("NewMessageReceived", new
                    {
                        messageId = message.Id,
                        chatId = senderId,
                        senderId = senderId,
                        senderName = sender?.FullName ?? "کاربر",
                        senderAvatar = sender?.AvatarUrl ?? "/images/default-avatar.png",
                        content = content,
                        sentAt = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        chatType = "private",
                        unreadCount = 1
                    });
                }

                // 4. تایید به فرستنده
                await Clients.Caller.SendAsync("MessageSent", new { success = true, messageId = message.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database Error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "خطا در ذخیره پیام");
            }
        }




        /// <summary>
        /// ارسال پیام گروهی
        /// </summary>
        public async Task SendGroupMessage(int groupId, string content)
        {
            var senderIdStr = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            var senderNameStr = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            if (!int.TryParse(senderIdStr, out int senderId))
                return;

            // ذخیره پیام
            var message = new Message
            {
                SenderId = senderId,
                GroupId = groupId,
                Content = content,
                MessageText = content,
                Type = MessageType.Text,
                SentAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // ارسال به تمام اعضای گروه
            var groupMembers = await _context.UserGroups
                .Where(ug => ug.GroupId == groupId && ug.IsActive)
                .Select(ug => ug.UserId)
                .ToListAsync();

            await Clients.Group($"group-{groupId}").SendAsync("ReceiveGroupMessage",
                new
                {
                    id = message.Id,
                    groupId = groupId,
                    senderId = senderId,
                    senderName = senderNameStr,
                    content = content,
                    sentAt = message.SentAt,
                    messageId = message.Id
                });
        }

        /// <summary>
        /// پیوستن به گروه
        /// </summary>
        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group-{groupId}");
        }

        /// <summary>
        /// ترک گروه
        /// </summary>
        public async Task LeaveGroup(int groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group-{groupId}");
        }

        /// <summary>
        /// ارسال تایپینگ اندیکیشن
        /// </summary>
        public async Task SendTypingNotification(int receiverId)
        {
            var senderNameStr = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            if (OnlineUsers.TryGetValue(receiverId, out var receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("UserTyping", senderNameStr);
            }
        }

        /// <summary>
        /// توقف تایپینگ
        /// </summary>
        public async Task SendStoppedTyping(int receiverId)
        {
            if (OnlineUsers.TryGetValue(receiverId, out var receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("UserStoppedTyping");
            }
        }

        /// <summary>
        /// دریافت وضعیت آنلاین تمام کاربران
        /// </summary>
        public async Task GetOnlineUsers()
        {
            await Clients.Caller.SendAsync("OnlineUsersList", OnlineUsers.Keys.ToList());
        }
    }
}