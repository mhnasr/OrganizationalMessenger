using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Web.Hubs;
using System.Security.Claims;

namespace OrganizationalMessenger.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext; // ✅ 
        public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
        [Route("Chat/GetChats")]
        public async Task<IActionResult> GetChats(string tab = "all")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var chats = await GetUserChats(userId.Value, tab);
            return Json(chats);
        }

        // دریافت پیامهای یک چت
        // دریافت پیامهای یک چت - با Pagination

        [HttpGet]
        public async Task<IActionResult> GetMessages(int? userId, int? groupId, int pageSize = 20, int? beforeMessageId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            IQueryable<Message> query = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.ReadReceipts)
                .Include(m => m.Attachments)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r.Sender)
                .Where(m => !m.IsSystemMessage);

            if (userId.HasValue)
            {
                query = query.Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId));
            }
            else if (groupId.HasValue)
            {
                query = query.Where(m => m.GroupId == groupId);
            }
            else
            {
                return BadRequest("userId or groupId is required");
            }

            if (beforeMessageId.HasValue)
            {
                var beforeMessage = await _context.Messages.FindAsync(beforeMessageId.Value);
                if (beforeMessage != null)
                {
                    query = query.Where(m => m.SentAt < beforeMessage.SentAt);
                }
            }

            // ✅ مرتب‌سازی DESC برای گرفتن جدیدترین‌ها
            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Take(pageSize)
                .ToListAsync();

            // ✅ Reverse برای نمایش قدیمی → جدید
            messages.Reverse();

            var showDeletedNoticeStr = await _context.SystemSettings
                .Where(s => s.Key == "ShowDeletedMessageNotice")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            bool showDeletedNotice = true;
            if (!string.IsNullOrEmpty(showDeletedNoticeStr))
            {
                showDeletedNotice = showDeletedNoticeStr.ToLower() == "true";
            }

            var result = messages
                .Where(m => showDeletedNotice || !m.IsDeleted)
                .Select(m => new
                {
                    m.Id,
                    MessageText = m.IsDeleted ? null : m.MessageText,
                    Content = m.IsDeleted ? null : m.Content,
                    m.Type,
                    m.SentAt,
                    m.DeliveredAt,
                    m.IsEdited,
                    m.EditedAt,
                    m.IsDeleted,
                    m.DeletedAt,
                    SenderId = m.SenderId,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    SenderAvatar = m.Sender.AvatarUrl ?? "/images/default-avatar.png",
                    IsDelivered = m.IsDelivered,
                    IsRead = m.SenderId == currentUserId.Value
                        ? m.ReadReceipts.Any(r => r.UserId == m.ReceiverId)
                        : m.ReadReceipts.Any(r => r.UserId == currentUserId.Value),
                    ReadAt = m.SenderId == currentUserId.Value
                        ? m.ReadReceipts
                            .Where(r => r.UserId == m.ReceiverId)
                            .Select(r => (DateTime?)r.ReadAt)
                            .FirstOrDefault()
                        : m.ReadReceipts
                            .Where(r => r.UserId == currentUserId.Value)
                            .Select(r => (DateTime?)r.ReadAt)
                            .FirstOrDefault(),
                    ReplyToMessageId = m.ReplyToMessageId,
                    ReplyToText = m.ReplyToMessage != null ? m.ReplyToMessage.Content : null,
                    ReplyToSenderName = m.ReplyToMessage != null
                        ? $"{m.ReplyToMessage.Sender.FirstName} {m.ReplyToMessage.Sender.LastName}"
                        : null,
                    Attachments = m.IsDeleted
                        ? new List<object>()
                        : m.Attachments
                            .Where(a => !a.IsDeleted)
                            .Select(a => (object)new
                            {
                                a.Id,
                                a.OriginalFileName,
                                a.FileUrl,
                                a.ThumbnailUrl,
                                FileType = a.FileType.ToString(),
                                a.FileSize,
                                a.Extension,
                                ReadableSize = a.ReadableFileSize,
                                a.Width,
                                a.Height,
                                a.Duration,
                                ReadableDuration = a.ReadableDuration
                            })
                            .ToList()
                })
                .ToList();

            return Json(new
            {
                messages = result,
                hasMore = messages.Count == pageSize
            });
        }
        // ✅ اصلاح SendMessage - با کپشن
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var senderId = GetCurrentUserId();
            if (senderId == null) return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.ReceiverId == null && request.GroupId == null && request.ChannelId == null)
                return BadRequest("ReceiverId or GroupId or ChannelId must be specified.");

            var sender = await _context.Users.FindAsync(senderId.Value);
            if (sender == null || !sender.IsActive)
                return Unauthorized();

            if (request.GroupId.HasValue)
            {
                var isGroupMember = await _context.UserGroups
                    .AnyAsync(ug => ug.UserId == senderId && ug.GroupId == request.GroupId && ug.IsActive);
                if (!isGroupMember) return Forbid();
            }

            // ✅ Log برای debug
            Console.WriteLine($"📝 MessageText received: {request.MessageText}");
            var now = DateTime.UtcNow;
            var message = new Message
            {
                SenderId = senderId.Value,
                ReceiverId = request.ReceiverId,
                GroupId = request.GroupId,
                ChannelId = request.ChannelId,
                MessageText = request.MessageText,      // ✅ کپشن
                Content = request.MessageText,          // ✅ کپشن (هر دو فیلد)
                Type = request.Type,
                SentAt = now,
                IsDelivered = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Message saved: Id={message.Id}, Content={message.Content}");

            // اتصال فایل به پیام
            if (request.FileAttachmentId.HasValue)
            {
                var file = await _context.FileAttachments
                    .FirstOrDefaultAsync(f => f.Id == request.FileAttachmentId.Value &&
                                             f.UploaderId == senderId.Value);
                if (file != null)
                {
                    file.MessageId = message.Id;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ File attached: FileId={file.Id}, MessageId={message.Id}");
                }
            }

            return Json(new
            {
                success = true,
                messageId = message.Id,
                sentAt = message.SentAt
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


        // ✅ موقتاً برای تست - 2 کاربر دیگه بساز



        // متد کمکی

        private async Task<dynamic> GetUserChats(int userId, string tab = "all")
        {
            var chats = new List<dynamic>();

            if (tab == "all" || tab == "private")
            {
                var users = await _context.Users
                    .Where(u => u.Id != userId && u.IsActive)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(50)
                    .ToListAsync();

                foreach (var user in users)
                {
                    var fullName = $"{user.FirstName} {user.LastName}".Trim();

                    var lastMessage = await _context.Messages
                        .Where(m =>
                            ((m.SenderId == userId && m.ReceiverId == user.Id) ||
                             (m.SenderId == user.Id && m.ReceiverId == userId)))
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefaultAsync();

                    // ✅ شمارش پیام‌های خوانده نشده
                    var unreadCount = await _context.Messages
                        .Where(m => m.SenderId == user.Id &&
                                   m.ReceiverId == userId &&
                                   !_context.MessageReads.Any(mr => mr.MessageId == m.Id && mr.UserId == userId))
                        .CountAsync();

                    chats.Add(new
                    {
                        type = "private",
                        id = user.Id,
                        name = fullName,
                        avatar = user.AvatarUrl ?? "/images/default-avatar.png",
                        isOnline = user.IsOnline,
                        lastMessage = lastMessage != null ?
                            (lastMessage.MessageText ?? lastMessage.Content ?? "") : "",
                        lastMessageTime = lastMessage?.SentAt ?? user.LastSeen ?? user.CreatedAt,
                        unreadCount,  // ✅ تعداد پیام‌های خوانده نشده
                        messageDirection = lastMessage?.SenderId == userId ? "sent" : "received"
                    });
                }
            }

            if (tab == "all" || tab == "group")
            {
                var groups = await _context.UserGroups
                    .Where(ug => ug.UserId == userId && ug.IsActive)
                    .Include(ug => ug.Group)
                    .Select(ug => ug.Group)
                    .ToListAsync();

                foreach (var group in groups)
                {
                    chats.Add(new
                    {
                        type = "group",
                        id = group.Id,
                        name = group.Name,
                        avatar = group.AvatarUrl ?? "/images/group-default.png",
                        lastMessage = "بدون پیام",
                        lastMessageTime = group.CreatedAt,
                        memberCount = 0,
                        unreadCount = 0
                    });
                }
            }

            return chats.OrderByDescending(c => c.lastMessageTime ?? DateTime.MinValue).ToList();
        }






        // ✅ DTO ها




        // ✅ فقط این 2 متد رو اضافه کن (بقیه رو دست نزن):

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessagesAsRead([FromBody] MarkAsReadRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var now = DateTime.Now;
            var messagesToMark = await _context.Messages
                .Where(m => request.MessageIds.Contains(m.Id) &&
                           m.ReceiverId == userId.Value &&  // فقط پیام‌های دریافتی
                           m.SenderId != userId.Value)      // 🚨 و نه ارسالی خودم!
                .ToListAsync();

            foreach (var message in messagesToMark)
            {
                if (!await _context.MessageReads.AnyAsync(mr =>
                    mr.MessageId == message.Id && mr.UserId == userId.Value))
                {
                    _context.MessageReads.Add(new MessageRead
                    {
                        MessageId = message.Id,
                        UserId = userId.Value,
                        ReadAt = now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, markedCount = messagesToMark.Count });
        }


        // ✅ کلاس DTO (فقط 1 بار - خط آخر کلاس)
        public class MarkAsReadRequest
        {
            public List<int> MessageIds { get; set; } = new();
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

        //*************************************************************
        // ✅ دریافت تنظیمات
        [HttpGet]
        public async Task<IActionResult> GetMessageSettings()
        {
            try
            {
                var allowEdit = await _context.SystemSettings
                    .Where(s => s.Key == "AllowMessageEdit")
                    .Select(s => s.Value.ToLower() == "true")
                    .FirstOrDefaultAsync();

                var allowDelete = await _context.SystemSettings
                    .Where(s => s.Key == "AllowMessageDelete")
                    .Select(s => s.Value.ToLower() == "true")
                    .FirstOrDefaultAsync();

                var editTimeLimitStr = await _context.SystemSettings
                    .Where(s => s.Key == "MessageEditTimeLimit")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                int editTimeLimit = 3600;
                if (!string.IsNullOrEmpty(editTimeLimitStr))
                {
                    int.TryParse(editTimeLimitStr, out editTimeLimit);
                }

                var deleteTimeLimitStr = await _context.SystemSettings
                    .Where(s => s.Key == "MessageDeleteTimeLimit")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                int deleteTimeLimit = 7200;
                if (!string.IsNullOrEmpty(deleteTimeLimitStr))
                {
                    int.TryParse(deleteTimeLimitStr, out deleteTimeLimit);
                }

                return Ok(new
                {
                    success = true,
                    allowEdit,
                    allowDelete,
                    editTimeLimit,
                    deleteTimeLimit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ✅ ویرایش پیام
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage([FromBody] EditMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.SenderId == userId.Value);

                if (message == null)
                    return NotFound(new { success = false, message = "پیام یافت نشد یا دسترسی ندارید" });

                var allowEdit = await _context.SystemSettings
                    .Where(s => s.Key == "AllowMessageEdit")
                    .Select(s => s.Value.ToLower() == "true")
                    .FirstOrDefaultAsync();

                if (!allowEdit)
                    return BadRequest(new { success = false, message = "ویرایش پیام غیرفعال است" });

                var editTimeLimitStr = await _context.SystemSettings
                    .Where(s => s.Key == "MessageEditTimeLimit")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                int editTimeLimit = 3600;
                if (!string.IsNullOrEmpty(editTimeLimitStr))
                {
                    int.TryParse(editTimeLimitStr, out editTimeLimit);
                }

                var elapsedSeconds = (DateTime.Now - message.SentAt).TotalSeconds;
                if (elapsedSeconds > editTimeLimit)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"زمان مجاز ویرایش ({editTimeLimit} ثانیه) گذشته است"
                    });
                }

                message.Content = request.NewContent;
                message.MessageText = request.NewContent;
                message.IsEdited = true;
                message.EditedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "پیام با موفقیت ویرایش شد",
                    editedAt = message.EditedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }






        // ✅ حذف پیام
        // ✅ حذف پیام - با تنظیمات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var message = await _context.Messages
                    .Include(m => m.Attachments)
                    .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.SenderId == userId.Value);

                if (message == null)
                    return NotFound(new { success = false, message = "پیام یافت نشد یا دسترسی ندارید" });

                var allowDelete = await _context.SystemSettings
                    .Where(s => s.Key == "AllowMessageDelete")
                    .Select(s => s.Value.ToLower() == "true")
                    .FirstOrDefaultAsync();

                if (!allowDelete)
                    return BadRequest(new { success = false, message = "حذف پیام غیرفعال است" });

                var deleteTimeLimitStr = await _context.SystemSettings
                    .Where(s => s.Key == "MessageDeleteTimeLimit")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                int deleteTimeLimit = 7200;
                if (!string.IsNullOrEmpty(deleteTimeLimitStr))
                {
                    int.TryParse(deleteTimeLimitStr, out deleteTimeLimit);
                }

                var elapsedSeconds = (DateTime.Now - message.SentAt).TotalSeconds;
                if (elapsedSeconds > deleteTimeLimit)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"زمان مجاز حذف ({deleteTimeLimit} ثانیه) گذشته است"
                    });
                }

                var showDeletedNoticeStr = await _context.SystemSettings
                    .Where(s => s.Key == "ShowDeletedMessageNotice")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                bool showDeletedNotice = true;
                if (!string.IsNullOrEmpty(showDeletedNoticeStr))
                {
                    showDeletedNotice = showDeletedNoticeStr.ToLower() == "true";
                }

                // ✅ ذخیره اطلاعات قبل از حذف
                var messageIdForNotification = message.Id;
                var receiverIdForNotification = message.ReceiverId;

                if (showDeletedNotice)
                {
                    // واتساپ mode: نمایش "پیام حذف شده"
                    message.Content = null;
                    message.MessageText = null;
                    message.IsDeleted = true;
                    message.DeletedAt = DateTime.Now;

                    foreach (var attachment in message.Attachments)
                    {
                        attachment.IsDeleted = true;
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // ✅ تلگرام mode: حذف کامل - اول اطلاعات را ذخیره کن
                    _context.Messages.Remove(message);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "پیام حذف شد",
                    showNotice = showDeletedNotice,
                    messageId = messageIdForNotification,  // ✅ اضافه کنید
                    receiverId = receiverIdForNotification  // ✅ اضافه کنید
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class DeleteMessageRequest
        {
            public int MessageId { get; set; }
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardMessages([FromBody] ForwardMessagesRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Attachments)
                    .Where(m => request.MessageIds.Contains(m.Id))
                    .ToListAsync();

                var forwardedMessageIds = new List<int>();

                foreach (var originalMessage in messages)
                {
                    var newMessage = new Message
                    {
                        SenderId = userId.Value,
                        ReceiverId = request.ReceiverId,
                        MessageText = originalMessage.MessageText,
                        Content = originalMessage.Content,
                        Type = originalMessage.Type,
                        SentAt = DateTime.Now,
                        IsDelivered = false,
                        IsEdited = false,
                        ForwardedFromMessageId = originalMessage.Id,
                        ForwardedFromUserId = originalMessage.SenderId,
                        Attachments = originalMessage.Attachments
                            .Where(a => !a.IsDeleted)
                            .Select(a => new FileAttachment
                            {
                                FileName = a.FileName,
                                OriginalFileName = a.OriginalFileName,
                                FilePath = a.FilePath,
                                FileUrl = a.FileUrl,
                                ContentType = a.ContentType,
                                Extension = a.Extension,
                                FileSize = a.FileSize,
                                FileType = a.FileType,
                                ThumbnailPath = a.ThumbnailPath,
                                ThumbnailUrl = a.ThumbnailUrl,
                                Width = a.Width,
                                Height = a.Height,
                                Duration = a.Duration,
                                UploaderId = userId.Value,
                                CreatedAt = DateTime.Now,
                                IsDeleted = false,
                                FileHash = a.FileHash,
                                IsScanned = a.IsScanned,
                                IsSafe = a.IsSafe,
                                ScannedAt = a.ScannedAt,
                                ScanResult = a.ScanResult,
                                DownloadCount = 0,
                                LastDownloadAt = null
                            })
                            .ToList()
                    };

                    _context.Messages.Add(newMessage);
                    await _context.SaveChangesAsync();

                    forwardedMessageIds.Add(newMessage.Id);

                    // ✅ اطلاع‌رسانی Real-time به گیرنده از طریق SignalR
                    try
                    {
                        var receiver = await _context.Users.FindAsync(request.ReceiverId);
                        if (receiver != null)
                        {
                            var messageDto = new
                            {
                                id = newMessage.Id,
                                senderId = newMessage.SenderId,
                                senderName = $"{User.Identity.Name}",
                                senderAvatar = "/images/default-avatar.png", // یا از دیتابیس بگیرید
                                content = newMessage.Content,
                                messageText = newMessage.MessageText,
                                type = newMessage.Type,
                                sentAt = newMessage.SentAt,
                                isDelivered = false,
                                isRead = false,
                                attachments = newMessage.Attachments.Select(a => new
                                {
                                    a.Id,
                                    a.OriginalFileName,
                                    a.FileUrl,
                                    a.ThumbnailUrl,
                                    fileType = a.FileType,
                                    a.FileSize,
                                    a.Extension,
                                    readableSize = a.ReadableFileSize,
                                    a.Width,
                                    a.Height
                                }).ToList()
                            };

                            await _hubContext.Clients.User(request.ReceiverId.ToString())
                                .SendAsync("ReceiveMessage", messageDto);
                        }
                    }
                    catch (Exception signalREx)
                    {
                        Console.WriteLine($"⚠️ SignalR notification error: {signalREx.Message}");
                    }
                }

                return Ok(new { success = true, message = "پیام‌ها ارجاع داده شدند", forwardedIds = forwardedMessageIds });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ForwardMessages error: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }




        public class ForwardMessagesRequest
        {
            public List<int> MessageIds { get; set; } = new();
            public int ReceiverId { get; set; }
        }



        





    }

    public class EditMessageRequest
    {
        public int MessageId { get; set; }
        public string NewContent { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        public int? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public int? ChannelId { get; set; }
        public string? MessageText { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public int? FileAttachmentId { get; set; }  // ✅ اضافه شد

    }
}
