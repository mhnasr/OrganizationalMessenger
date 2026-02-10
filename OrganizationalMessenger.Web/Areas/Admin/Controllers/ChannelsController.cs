using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Web.Models.ViewModels;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChannelsController : BaseAdminController
    {
        private readonly ApplicationDbContext _context;

        public ChannelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Channels
        public async Task<IActionResult> Index(string searchTerm = "", bool? isActive = null)
        {
            var query = _context.Channels
                .Include(c => c.Creator)
                .Include(c => c.UserChannels)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm) ||
                                        (c.Description != null && c.Description.Contains(searchTerm)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            var channels = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ChannelListViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatorName = c.Creator.FirstName + " " + c.Creator.LastName,
                    SubscriberCount = c.UserChannels.Count(uc => uc.IsActive),
                    IsActive = c.IsActive,
                    IsPublic = c.IsPublic,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsActive = isActive;

            return View(channels);
        }

        // GET: Admin/Channels/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var channel = await _context.Channels
                .Include(c => c.Creator)
                .Include(c => c.UserChannels.Where(uc => uc.IsActive))
                    .ThenInclude(uc => uc.User)
                .Include(c => c.Messages.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (channel == null)
            {
                TempData["ErrorMessage"] = "کانال یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MessageCount = channel.Messages.Count;
            ViewBag.SubscriberCount = channel.UserChannels.Count(uc => uc.IsActive);
            ViewBag.AdminCount = channel.UserChannels.Count(uc => uc.IsActive && uc.IsAdmin);
            ViewBag.PublisherCount = channel.UserChannels.Count(uc => uc.IsActive && uc.CanPost);

            return View(channel);
        }

        // GET: Admin/Channels/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return View(new ChannelCreateViewModel
            {
                IsActive = true,
                IsPublic = false
            });
        }

        // POST: Admin/Channels/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChannelCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // بررسی یکتا بودن نام
                var existingChannel = await _context.Channels
                    .AnyAsync(c => c.Name == model.Name);

                if (existingChannel)
                {
                    ModelState.AddModelError("Name", "کانالی با این نام قبلاً وجود دارد.");
                    ViewBag.Users = await _context.Users
                        .Where(u => u.IsActive && !u.IsDeleted)
                        .ToListAsync();
                    return View(model);
                }

                var channel = new Channel
                {
                    Name = model.Name,
                    Description = model.Description,
                    CreatorId = model.CreatorId,
                    IsPublic = model.IsPublic,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Channels.Add(channel);
                await _context.SaveChangesAsync();

                // افزودن سازنده به عنوان Owner
                var userChannel = new UserChannel
                {
                    UserId = model.CreatorId,
                    ChannelId = channel.Id,
                    IsAdmin = true,
                    CanPost = true,
                    Role = ChannelRole.Owner,
                    JoinedAt = DateTime.Now,
                    IsActive = true
                };

                _context.UserChannels.Add(userChannel);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "کانال با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/Channels/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                TempData["ErrorMessage"] = "کانال یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ChannelEditViewModel
            {
                Id = channel.Id,
                Name = channel.Name,
                Description = channel.Description,
                IsPublic = channel.IsPublic,
                IsActive = channel.IsActive,
                CreatorId = channel.CreatorId,
                CreatedAt = channel.CreatedAt
            };

            return View(model);
        }

        // POST: Admin/Channels/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChannelEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var channel = await _context.Channels.FindAsync(id);

                if (channel == null)
                {
                    TempData["ErrorMessage"] = "کانال یافت نشد.";
                    return RedirectToAction(nameof(Index));
                }

                // بررسی یکتا بودن نام
                if (channel.Name != model.Name)
                {
                    var nameExists = await _context.Channels
                        .AnyAsync(c => c.Name == model.Name && c.Id != id);

                    if (nameExists)
                    {
                        ModelState.AddModelError("Name", "کانالی با این نام قبلاً وجود دارد.");
                        return View(model);
                    }
                }

                channel.Name = model.Name;
                channel.Description = model.Description;
                channel.IsPublic = model.IsPublic;
                channel.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "کانال با موفقیت ویرایش شد.";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Admin/Channels/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel != null)
            {
                channel.IsActive = false;
                channel.IsDeleted = true;
                channel.DeletedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "کانال با موفقیت حذف شد.";
            }
            else
            {
                TempData["ErrorMessage"] = "کانال یافت نشد.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Channels/Subscribers/5
        public async Task<IActionResult> Subscribers(int id)
        {
            var channel = await _context.Channels
                .Include(c => c.UserChannels.Where(uc => uc.IsActive))
                    .ThenInclude(uc => uc.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (channel == null)
            {
                TempData["ErrorMessage"] = "کانال یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var subscriberIds = channel.UserChannels
                .Where(uc => uc.IsActive)
                .Select(uc => uc.UserId)
                .ToList();

            ViewBag.AvailableUsers = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted && !subscriberIds.Contains(u.Id))
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            ViewBag.ChannelId = id;
            ViewBag.ChannelName = channel.Name;

            return View(channel);
        }

        // POST: Admin/Channels/AddSubscriber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubscriber(int channelId, int userId, bool canPost = false, bool isAdmin = false)
        {
            var channel = await _context.Channels.FindAsync(channelId);

            if (channel == null)
            {
                TempData["ErrorMessage"] = "کانال یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var existingSubscription = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId);

            if (existingSubscription != null)
            {
                if (existingSubscription.IsActive)
                {
                    TempData["ErrorMessage"] = "این کاربر قبلاً عضو کانال است.";
                    return RedirectToAction(nameof(Subscribers), new { id = channelId });
                }
                else
                {
                    existingSubscription.IsActive = true;
                    existingSubscription.CanPost = canPost;
                    existingSubscription.IsAdmin = isAdmin;
                    existingSubscription.JoinedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "کاربر با موفقیت به کانال اضافه شد.";
                    return RedirectToAction(nameof(Subscribers), new { id = channelId });
                }
            }

            var role = isAdmin ? ChannelRole.Admin : (canPost ? ChannelRole.Publisher : ChannelRole.Subscriber);

            var userChannel = new UserChannel
            {
                UserId = userId,
                ChannelId = channelId,
                IsAdmin = isAdmin,
                CanPost = canPost,
                Role = role,
                JoinedAt = DateTime.Now,
                IsActive = true
            };

            _context.UserChannels.Add(userChannel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "کاربر با موفقیت به کانال اضافه شد.";
            return RedirectToAction(nameof(Subscribers), new { id = channelId });
        }

        // POST: Admin/Channels/RemoveSubscriber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSubscriber(int channelId, int userId)
        {
            var userChannel = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId);

            if (userChannel != null)
            {
                if (userChannel.Role == ChannelRole.Owner)
                {
                    TempData["ErrorMessage"] = "مالک کانال قابل حذف نیست.";
                    return RedirectToAction(nameof(Subscribers), new { id = channelId });
                }

                userChannel.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "کاربر از کانال حذف شد.";
            }

            return RedirectToAction(nameof(Subscribers), new { id = channelId });
        }

        // POST: Admin/Channels/TogglePublisher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublisher(int channelId, int userId)
        {
            var userChannel = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId && uc.IsActive);

            if (userChannel != null)
            {
                userChannel.CanPost = !userChannel.CanPost;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = userChannel.CanPost
                    ? "دسترسی ارسال پست به کاربر داده شد."
                    : "دسترسی ارسال پست کاربر لغو شد.";
            }

            return RedirectToAction(nameof(Subscribers), new { id = channelId });
        }

        // POST: Admin/Channels/ToggleAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(int channelId, int userId)
        {
            var userChannel = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId && uc.IsActive);

            if (userChannel != null)
            {
                if (userChannel.Role == ChannelRole.Owner)
                {
                    TempData["ErrorMessage"] = "نقش مالک کانال قابل تغییر نیست.";
                    return RedirectToAction(nameof(Subscribers), new { id = channelId });
                }

                userChannel.IsAdmin = !userChannel.IsAdmin;
                userChannel.Role = userChannel.IsAdmin ? ChannelRole.Admin :
                                   (userChannel.CanPost ? ChannelRole.Publisher : ChannelRole.Subscriber);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = userChannel.IsAdmin
                    ? "کاربر به ادمین ارتقا یافت."
                    : "دسترسی ادمین کاربر لغو شد.";
            }

            return RedirectToAction(nameof(Subscribers), new { id = channelId });
        }

        // متد کمکی
        private async Task<bool> ChannelExistsAsync(int id)
        {
            return await _context.Channels.AnyAsync(e => e.Id == id);
        }
    }
}
