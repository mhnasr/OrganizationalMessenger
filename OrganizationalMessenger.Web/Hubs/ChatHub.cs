// ✅ ChatHub.cs - SignalR Hub برای Real-time Communication

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace OrganizationalMessenger.Web.Hubs
{
    [Authorize] // فقط کاربران احراز هویت شده
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        // 🚨 Thread-safe dictionary برای نگهداری ConnectionId های هر کاربر
        private static readonly ConcurrentDictionary<int, List<string>> _userConnections = new();

        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ وقتی کاربر متصل شد
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                await base.OnConnectedAsync();
                return;
            }

            _logger.LogInformation($"✅ User {userId} connected: {Context.ConnectionId}");

            // ذخیره ConnectionId های کاربر
            _userConnections.AddOrUpdate(userId,
                new List<string> { Context.ConnectionId },
                (key, list) => { list.Add(Context.ConnectionId); return list; });

            // به‌روزرسانی IsOnline در دیتابیس
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.Now;
                await _context.SaveChangesAsync();

                // اطلاع به سایر کاربران
                await Clients.Others.SendAsync("UserOnline", userId);
            }

            // 🚨 Mark Sent → Delivered برای پیام‌های 24 ساعت اخیر
            await MarkReceivedMessagesAsDelivered(userId);

            await base.OnConnectedAsync();
        }

        // ✅ وقتی کاربر قطع شد
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId > 0 && _userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);

                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.IsOnline = false;
                        user.LastSeen = DateTime.Now;
                        await _context.SaveChangesAsync();

                        // اطلاع به سایر کاربران
                        await Clients.Others.SendAsync("UserOffline", userId, DateTime.Now);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ✅ ارسال پیام خصوصی
        // ✅ ارسال پیام با Reply
        public async Task SendPrivateMessage(int receiverId, string messageText, int? replyToMessageId = null)
        {
            var senderId = GetUserId();
            if (senderId == null) return;

            try
            {
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = messageText,
                    MessageText = messageText,
                    Type = MessageType.Text,
                    SentAt = DateTime.UtcNow, // ✅ UTC
                    IsDelivered = false,
                    ReplyToMessageId = replyToMessageId
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var sender = await _context.Users.FindAsync(senderId);

                var messageDto = new
                {
                    id = message.Id,
                    messageId = message.Id,
                    chatId = receiverId,
                    senderId = message.SenderId,
                    senderName = $"{sender.FirstName} {sender.LastName}",
                    senderAvatar = sender.AvatarUrl ?? "/images/default-avatar.png",
                    content = message.Content,
                    messageText = message.MessageText,
                    type = message.Type,
                    sentAt = message.SentAt.ToString("o"), // ✅ ISO 8601
                    isDelivered = false,
                    isRead = false,
                    isEdited = false,
                    replyToMessageId = message.ReplyToMessageId,
                    replyToText = message.ReplyToMessage?.Content,
                    replyToSenderName = message.ReplyToMessage != null
                        ? $"{message.ReplyToMessage.Sender.FirstName} {message.ReplyToMessage.Sender.LastName}"
                        : null,
                    attachments = new List<object>()
                };

                Console.WriteLine($"✅ Message {message.Id} - SentAt: {messageDto.sentAt}");

                await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", messageDto);
                await Clients.Caller.SendAsync("MessageSent", messageDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SendPrivateMessage error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "خطا در ارسال پیام");
            }
        }

        // ✅ تأیید Delivered از Frontend
        public async Task ConfirmDelivery(long messageId)
        {
            var userId = GetUserId();
            var message = await _context.Messages.FindAsync(messageId);

            if (message?.ReceiverId == userId && !message.IsDelivered)
            {
                message.IsDelivered = true;
                message.DeliveredAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await Clients.All.SendAsync("MessageDelivered", new
                {
                    messageId,
                    deliveredAt = message.DeliveredAt
                });
            }
        }

        // 🚨 MarkAsRead - درج رکورد در جدول MessageReads
        public async Task MarkAsRead(int messageId)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            // 🚨 چک تکراری - اگر قبلاً خوانده → خارج شو
            if (await _context.MessageReads.AnyAsync(mr => mr.MessageId == messageId && mr.UserId == userId))
                return;

            // 🚨 درج رکورد جدید در MessageReads ← PROBLEM!
            var read = new MessageRead
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.Now
            };

            _context.MessageReads.Add(read);
            await _context.SaveChangesAsync();

            // اطلاع به فرستنده
            await Clients.All.SendAsync("MessageRead", new
            {
                messageId,
                userId,
                readAt = read.ReadAt.ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }

        // Typing indicator
        public async Task SendTyping(int receiverId)
        {
            if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
            {
                foreach (var conn in receiverConnections)
                    await Clients.Client(conn).SendAsync("UserTyping", "کاربر در حال تایپ...");
            }
        }

        public async Task SendStoppedTyping(int receiverId)
        {
            if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
            {
                foreach (var conn in receiverConnections)
                    await Clients.Client(conn).SendAsync("UserStoppedTyping");
            }
        }

        // علامت‌گذاری پیام‌های 24 ساعت اخیر به Delivered
        private async Task MarkReceivedMessagesAsDelivered(int userId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.ReceiverId == userId &&
                               !m.IsDelivered &&
                               m.SentAt > DateTime.Now.AddHours(-24))
                    .ToListAsync();

                foreach (var msg in messages)
                {
                    msg.IsDelivered = true;
                    msg.DeliveredAt = DateTime.Now;
                }

                if (messages.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"📦 {messages.Count} messages marked Delivered for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkReceivedMessagesAsDelivered error");
            }
        }

        private int GetUserId()
        {
            return int.TryParse(
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out int id) ? id : 0;
        }

        // 🚨 NotifyMessagesRead - از Frontend صدا زده می‌شود (selectChat)
        public async Task NotifyMessagesRead(List<int> messageIds)
        {
            var userId = GetUserId();
            var now = DateTime.Now;

            // 🚨 درج رکوردهای MessageReads برای همه messageIds ← MAIN PROBLEM!
            foreach (var msgId in messageIds)
            {
                if (!await _context.MessageReads.AnyAsync(mr => mr.MessageId == msgId && mr.UserId == userId))
                {
                    _context.MessageReads.Add(new MessageRead
                    {
                        MessageId = msgId,
                        UserId = userId,
                        ReadAt = now
                    });
                }
            }
            await _context.SaveChangesAsync();

            // اطلاع به فرستنده‌ها
            foreach (var msgId in messageIds)
            {
                var message = await _context.Messages.FindAsync(msgId);
                if (message != null)
                {
                    await Clients.All.SendAsync("MessageRead", new
                    {
                        messageId = msgId,
                        readAt = now.ToString("yyyy-MM-ddTHH:mm:ss")
                    });
                }
            }
        }


        // ✅ ارسال پیام خصوصی با فایل
        // ✅ ارسال پیام خصوصی با فایل - اصلاح شده
        public async Task SendPrivateMessageWithFile(int receiverId, string messageText, int messageId, int fileAttachmentId)
        {
            var senderId = GetUserId();
            if (senderId == 0) return;

            try
            {
                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Attachments.Where(a => !a.IsDeleted))
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    _logger.LogError($"❌ Message {messageId} not found");
                    return;
                }

                var file = message.Attachments.FirstOrDefault(a => a.Id == fileAttachmentId);
                if (file == null)
                {
                    _logger.LogError($"❌ File {fileAttachmentId} not found");
                    return;
                }

                // ✅ تبدیل به UTC اگر Local است
                var sentAtUtc = message.SentAt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(message.SentAt, DateTimeKind.Utc)
                    : message.SentAt.ToUniversalTime();

                var messageDto = new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    senderName = $"{message.Sender.FirstName} {message.Sender.LastName}",
                    senderAvatar = message.Sender.AvatarUrl ?? "/images/default-avatar.png",
                    content = message.Content,
                    messageText = message.MessageText,
                    type = message.Type,
                    sentAt = sentAtUtc.ToString("o"), // ✅ ISO 8601 با "o"
                    isDelivered = false,
                    isRead = false,
                    isEdited = message.IsEdited,
                    editedAt = message.EditedAt,
                    chatId = receiverId,
                    attachments = new[]
                    {
                new
                {
                    id = file.Id,
                    originalFileName = file.OriginalFileName,
                    fileUrl = file.FileUrl,
                    thumbnailUrl = file.ThumbnailUrl,
                    fileType = file.FileType,
                    fileSize = file.FileSize,
                    extension = file.Extension,
                    readableSize = file.ReadableFileSize,
                    width = file.Width,
                    height = file.Height,
                    duration = file.Duration,
                    readableDuration = file.ReadableDuration
                }
            }
                };

                Console.WriteLine($"✅ Message {message.Id} - SentAt (Local): {message.SentAt}");
                Console.WriteLine($"✅ Message {message.Id} - SentAt (UTC): {messageDto.sentAt}");

                // تأیید برای فرستنده
                await Clients.Caller.SendAsync("MessageSent", messageDto);

                // ارسال به گیرنده
                if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
                {
                    foreach (var connectionId in receiverConnections)
                    {
                        await Clients.Client(connectionId).SendAsync("ReceiveMessage", messageDto);
                    }

                    message.IsDelivered = true;
                    message.DeliveredAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    await Clients.All.SendAsync("MessageDelivered", new
                    {
                        messageId = message.Id,
                        deliveredAt = message.DeliveredAt?.ToString("yyyy-MM-ddTHH:mm:ss")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ SendPrivateMessageWithFile error: {ex.Message}");
            }
        }
        // ✅ اطلاع‌رسانی ویرایش پیام
        // ✅ اطلاع‌رسانی ویرایش پیام
        public async Task NotifyMessageEdited(int messageId, string newContent, DateTime editedAt)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            var message = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null) return;

            // اطلاع به همه (فرستنده + گیرنده)
            await Clients.All.SendAsync("MessageEdited", new
            {
                messageId,
                newContent,
                editedAt = editedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                senderId = userId
            });

            _logger.LogInformation($"✅ Message {messageId} edited by {userId}");
        }





        // ✅ اطلاع‌رسانی حذف پیام - با تنظیمات
        // ✅ اطلاع‌رسانی حذف پیام - با ارسال به گیرنده
        // ✅ اطلاع‌رسانی حذف پیام - بدون query
        public async Task NotifyMessageDeleted(int messageId, bool showNotice, int? receiverId)
        {
            var userId = GetUserId();
            if (userId == 0) return;

            try
            {
                // ✅ لاگ برای debug
                _logger.LogInformation($"🗑️ NotifyMessageDeleted: messageId={messageId}, sender={userId}, receiver={receiverId}, showNotice={showNotice}");

                // ✅ چک کردن آنلاین بودن گیرنده
                if (receiverId.HasValue && _userConnections.TryGetValue(receiverId.Value, out var receiverConnections))
                {
                    _logger.LogInformation($"📡 Receiver {receiverId} has {receiverConnections.Count} active connections");

                    // ارسال به گیرنده
                    foreach (var connectionId in receiverConnections)
                    {
                        await Clients.Client(connectionId).SendAsync("MessageDeleted", new
                        {
                            messageId,
                            showNotice,
                            deletedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                            senderId = userId,
                            receiverId
                        });

                        _logger.LogInformation($"📤 Sent MessageDeleted to connection: {connectionId}");
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ Receiver {receiverId} is offline or not found in connections");
                }

                // ✅ ارسال به فرستنده (برای تأیید)
                await Clients.Caller.SendAsync("MessageDeleted", new
                {
                    messageId,
                    showNotice,
                    deletedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    senderId = userId,
                    receiverId
                });

                _logger.LogInformation($"✅ Message {messageId} deletion notified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ NotifyMessageDeleted error: {ex.Message}");
            }
        }



    }
}
