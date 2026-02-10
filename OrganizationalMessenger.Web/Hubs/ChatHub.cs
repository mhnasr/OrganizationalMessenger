using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Domain.Entities;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private static readonly ConcurrentDictionary<int, List<string>> _userConnections = new();

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("UserId")?.Value ??
                         Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }

    // 🔥 SendMessage - Private Chat (SenderId + ReceiverId)
    public async Task SendMessage(int receiverId, string content)
    {
        try
        {
            var senderId = GetUserId();
            if (senderId == 0 || senderId == receiverId)
            {
                await Clients.Caller.SendAsync("Error", "خطا در ارسال");
                return;
            }

            Console.WriteLine($"📨 [{senderId}→{receiverId}] {content.Substring(0, Math.Min(30, content.Length))}");

            // ✅ Message جدید (سازگار با Entity شما)
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageText = content.Trim(),
                Content = content.Trim(), // هر دو فیلد
                SentAt = DateTime.Now,
                IsDelivered = false,
                
                IsDeleted = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var sender = await _context.Users.FindAsync(senderId);
            var receiverOnline = _userConnections.ContainsKey(receiverId);

            // ✅ Auto-Delivered اگر گیرنده آنلاین
            if (receiverOnline)
            {
                message.IsDelivered = true;
                message.DeliveredAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            var messageDto = new
            {
                messageId = message.Id,
                chatId = receiverId, // chatId = receiverId برای Private
                senderId = message.SenderId,
                senderName = sender?.FirstName + " " + sender?.LastName,
                content = message.MessageText,
                sentAt = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                status = receiverOnline ? 2 : 1, // 1=Sent, 2=Delivered
                isOwn = true,
                isDelivered = receiverOnline
            };

            // 📤 ارسال به فرستنده
            await Clients.Caller.SendAsync("ReceiveMessage", messageDto);

            // 📤 ارسال به گیرنده (اگر آنلاین)
            if (receiverOnline)
            {
                await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", messageDto);
            }

            Console.WriteLine($"✅ Message {message.Id} sent (status: {messageDto.status})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SendMessage: {ex.Message}");
            await Clients.Caller.SendAsync("Error", "خطا در ارسال");
        }
    }

    // ✅ Typing
    public async Task Typing(int receiverId, bool isTyping)
    {
        await Clients.User(receiverId.ToString()).SendAsync("UserTyping", receiverId, GetUserId(), isTyping);
    }

    // ✅ Join Chat (Private = receiverId)
    public async Task JoinChat(int receiverId)
    {
        var groupName = $"chat_{receiverId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Mark previous messages as delivered
        await MarkPendingMessagesAsDelivered(GetUserId(), receiverId);

        Console.WriteLine($"👥 Joined private chat with {receiverId}");
    }

    // ✅ Online/Offline - کامل
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == 0) return;

        // Connection tracking
        if (!_userConnections.TryGetValue(userId, out var connections))
        {
            connections = new();
            _userConnections[userId] = connections;
        }
        connections.Add(Context.ConnectionId);

        // Update DB
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsOnline = true;
            user.LastSeen = DateTime.Now;
            await _context.SaveChangesAsync();
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        Console.WriteLine($"✅ User {userId} ONLINE");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId == 0 || !_userConnections.TryGetValue(userId, out var connections))
            return;

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
                await Clients.Others.SendAsync("UserOffline", userId);
            }
            Console.WriteLine($"❌ User {userId} OFFLINE");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ✅ Mark Pending Messages as Delivered
    private async Task MarkPendingMessagesAsDelivered(int receiverId, int senderId)
    {
        var pending = await _context.Messages
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId &&
                       !m.IsDelivered && !m.IsDeleted)
            .ToListAsync();

        foreach (var msg in pending)
        {
            msg.IsDelivered = true;
            msg.DeliveredAt = DateTime.Now;
        }

        if (pending.Any()) await _context.SaveChangesAsync();
    }
}
