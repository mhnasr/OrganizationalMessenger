using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using System.DirectoryServices.ActiveDirectory;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private static Dictionary<string, string> _userConnections = new();

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[Context.ConnectionId] = userId;

            // آپدیت وضعیت آنلاین کاربر
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.Now;
                await _context.SaveChangesAsync();

                // اطلاع به دیگران
                await Clients.All.SendAsync("UserOnlineStatusChanged", userId, true);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (_userConnections.TryGetValue(Context.ConnectionId, out var userId))
        {
            _userConnections.Remove(Context.ConnectionId);

            // آپدیت وضعیت آفلاین
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.Now;
                await _context.SaveChangesAsync();

                // اطلاع به دیگران
                await Clients.All.SendAsync("UserOnlineStatusChanged", userId, false);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string messageText, int? receiverId = null, int? groupId = null)
    {
        var userId = int.Parse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (userId == 0 || string.IsNullOrEmpty(messageText))
            return;

        var message = new Message
        {
            SenderId = userId,
            ReceiverId = receiverId,
            GroupId = groupId,
            Content = messageText,
            MessageText = messageText,
            Type = MessageType.Text,
            SentAt = DateTime.Now,
            IsDelivered = true
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // ارسال پیام
        if (receiverId.HasValue)
        {
            // پیام خصوصی
            await Clients.User(receiverId.Value.ToString())
                .SendAsync("ReceiveMessage", userId, "", messageText);
        }
        else if (groupId.HasValue)
        {
            // پیام ��روهی
            await Clients.All.SendAsync("ReceiveMessage", userId, "", messageText);
        }

        // تیک‌های تک
        await Clients.Caller.SendAsync("MessageSent", message.Id);
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        var userId = int.Parse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var messageRead = new MessageRead
        {
            MessageId = messageId,
            UserId = userId,
            ReadAt = DateTime.Now
        };

        _context.MessageReads.Add(messageRead);
        await _context.SaveChangesAsync();

        // تیک دوم سبز
        await Clients.All.SendAsync("MessageRead", messageId, userId);
    }
}