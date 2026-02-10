using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Infrastructure.Data;
using System.Security.Claims;

[Authorize]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _context;

    public ChatController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = GetCurrentUserId();
        ViewBag.CurrentUserId = currentUserId;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new {
                userId = u.Id,
                fullName = u.FirstName + " " + u.LastName,
                username = u.Username,
                profileImage = u.AvatarUrl
            })
            .FirstOrDefaultAsync();
        return Json(user);
    }

    [HttpGet]
    [Route("api/[controller]/users")] // ✅ Route دقیق
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            Console.WriteLine($"🔍 GetUsers called for user: {currentUserId}");

            var users = await _context.Users
                .AsNoTracking() // ✅ Performance
                .Where(u => u.Id != currentUserId && u.IsActive == true)
                .OrderByDescending(u => u.IsOnline)
                .ThenBy(u => u.FirstName)
                .Take(50) // ✅ Limit
                .Select(u => new
                {
                    userId = u.Id,
                    fullName = $"{u.FirstName} {u.LastName}".Trim(),
                    username = u.Username ?? "",
                    isOnline = u.IsOnline,
                    profileImage = u.AvatarUrl ?? ""
                })
                .ToListAsync();

            Console.WriteLine($"✅ GetUsers SUCCESS: {users.Count} users");
            return Json(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetUsers ERROR: {ex.Message}");
            return Json(new List<object>()); // ✅ Empty list instead of error
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetMessages(int receiverId, int? lastMessageId = null)
    {
        var currentUserId = GetCurrentUserId();

        var query = _context.Messages
            .Where(m => !m.IsDeleted &&
                       ((m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                        (m.SenderId == receiverId && m.ReceiverId == currentUserId)));

        if (lastMessageId.HasValue)
            query = query.Where(m => m.Id < lastMessageId.Value);

        var messages = await query
            .OrderBy(m => m.SentAt)
            .Take(50)
            .Select(m => new
            {
                messageId = m.Id,
                senderId = m.SenderId,
                senderName = m.Sender.FirstName + " " + m.Sender.LastName,
                content = m.MessageText ?? m.Content,
                sentAt = m.SentAt,
                isDelivered = m.IsDelivered,
                
            })
            .ToListAsync();

        return Json(messages);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead(int receiverId)
    {
        var currentUserId = GetCurrentUserId();
        var messages = await _context.Messages
            .Where(m => !m.IsDeleted && m.SenderId == receiverId &&
                       m.ReceiverId == currentUserId )
            .ToListAsync();

       

        await _context.SaveChangesAsync();
        return Json(new { count = messages.Count });
    }
}
