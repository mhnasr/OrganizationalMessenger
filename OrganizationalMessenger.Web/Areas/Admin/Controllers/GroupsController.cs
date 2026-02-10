using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Web.Models.ViewModels;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class GroupsController : BaseAdminController
    {
        private readonly ApplicationDbContext _context;

        public GroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Groups
        public async Task<IActionResult> Index(string searchTerm = "", bool? isActive = null)
        {
            var query = _context.Groups
                .Include(g => g.Creator)
                .Include(g => g.UserGroups)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(g => g.Name.Contains(searchTerm) ||
                                        (g.Description != null && g.Description.Contains(searchTerm)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            var groups = await query
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new GroupListViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CreatorName = g.Creator.FirstName + " " + g.Creator.LastName,
                    MemberCount = g.UserGroups.Count(ug => ug.IsActive),
                    IsActive = g.IsActive,
                    IsPublic = g.IsPublic,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsActive = isActive;

            return View(groups);
        }

        // GET: Admin/Groups/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Creator)
                .Include(g => g.UserGroups)
                    .ThenInclude(ug => ug.User)
                .Include(g => g.Messages.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MessageCount = group.Messages.Count;
            ViewBag.MemberCount = group.UserGroups.Count(ug => ug.IsActive);
            ViewBag.AdminCount = group.UserGroups.Count(ug => ug.IsActive && ug.IsAdmin);

            return View(group);
        }

        // GET: Admin/Groups/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return View(new GroupCreateViewModel
            {
                MaxMembers = 100,
                IsActive = true,
                IsPublic = false
            });
        }

        // POST: Admin/Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // بررسی یکتا بودن نام گروه
                var existingGroup = await _context.Groups
                    .AnyAsync(g => g.Name == model.Name);

                if (existingGroup)
                {
                    ModelState.AddModelError("Name", "گروهی با این نام قبلاً وجود دارد.");
                    ViewBag.Users = await _context.Users
                        .Where(u => u.IsActive && !u.IsDeleted)
                        .ToListAsync();
                    return View(model);
                }

                var group = new Group
                {
                    Name = model.Name,
                    Description = model.Description,
                    CreatorId = model.CreatorId,
                    MaxMembers = model.MaxMembers,
                    IsPublic = model.IsPublic,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                // افزودن سازنده به عنوان اولین عضو و Owner
                var userGroup = new UserGroup
                {
                    UserId = model.CreatorId,
                    GroupId = group.Id,
                    IsAdmin = true,
                    Role = GroupRole.Owner,
                    JoinedAt = DateTime.Now,
                    IsActive = true
                };

                _context.UserGroups.Add(userGroup);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "گروه با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/Groups/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _context.Groups.FindAsync(id);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var model = new GroupEditViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                MaxMembers = group.MaxMembers,
                IsPublic = group.IsPublic,
                IsActive = group.IsActive,
                CreatorId = group.CreatorId,
                CreatedAt = group.CreatedAt
            };

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            return View(model);
        }

        // POST: Admin/Groups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GroupEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var group = await _context.Groups.FindAsync(id);

                    if (group == null)
                    {
                        TempData["ErrorMessage"] = "گروه یافت نشد.";
                        return RedirectToAction(nameof(Index));
                    }

                    // بررسی یکتا بودن نام (اگر تغییر کرده)
                    if (group.Name != model.Name)
                    {
                        var nameExists = await _context.Groups
                            .AnyAsync(g => g.Name == model.Name && g.Id != id);

                        if (nameExists)
                        {
                            ModelState.AddModelError("Name", "گروهی با این نام قبلاً وجود دارد.");
                            ViewBag.Users = await _context.Users
                                .Where(u => u.IsActive && !u.IsDeleted)
                                .ToListAsync();
                            return View(model);
                        }
                    }

                    group.Name = model.Name;
                    group.Description = model.Description;
                    group.MaxMembers = model.MaxMembers;
                    group.IsPublic = model.IsPublic;
                    group.IsActive = model.IsActive;
                    group.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "گروه با موفقیت ویرایش شد.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await GroupExistsAsync(model.Id))
                    {
                        TempData["ErrorMessage"] = "گروه یافت نشد.";
                        return RedirectToAction(nameof(Index));
                    }
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            return View(model);
        }

        // POST: Admin/Groups/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .Include(g => g.Messages)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group != null)
            {
                // حذف نرم (Soft Delete)
                group.IsActive = false;
                group.IsDeleted = true;
                group.DeletedAt = DateTime.Now;

                // غیرفعال کردن تمام عضویت‌ها
                foreach (var ug in group.UserGroups)
                {
                    ug.IsActive = false;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "گروه با موفقیت حذف شد.";
            }
            else
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Groups/HardDelete/5 - حذف کامل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id)
        {
            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .Include(g => g.Messages)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group != null)
            {
                // حذف تمام پیام‌های گروه
                _context.Messages.RemoveRange(group.Messages);

                // حذف تمام عضویت‌ها
                _context.UserGroups.RemoveRange(group.UserGroups);

                // حذف گروه
                _context.Groups.Remove(group);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "گروه به طور کامل حذف شد.";
            }
            else
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Groups/Members/5
        public async Task<IActionResult> Members(int id)
        {
            var group = await _context.Groups
                .Include(g => g.UserGroups.Where(ug => ug.IsActive))
                    .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            // کاربرانی که عضو این گروه نیستند
            var memberUserIds = group.UserGroups
                .Where(ug => ug.IsActive)
                .Select(ug => ug.UserId)
                .ToList();

            ViewBag.AvailableUsers = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted && !memberUserIds.Contains(u.Id))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            ViewBag.GroupId = id;
            ViewBag.GroupName = group.Name;
            ViewBag.MaxMembers = group.MaxMembers;
            ViewBag.CurrentMemberCount = memberUserIds.Count;

            return View(group);
        }

        // POST: Admin/Groups/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int groupId, int userId, bool isAdmin = false)
        {
            var group = await _context.Groups
                .Include(g => g.UserGroups.Where(ug => ug.IsActive))
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            // بررسی ظرفیت گروه
            var currentMemberCount = group.UserGroups.Count(ug => ug.IsActive);
            if (currentMemberCount >= group.MaxMembers)
            {
                TempData["ErrorMessage"] = $"ظرفیت گروه ({group.MaxMembers} نفر) تکمیل است.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            // بررسی وجود کاربر
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                TempData["ErrorMessage"] = "کاربر یافت نشد یا غیرفعال است.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            // بررسی عضویت قبلی
            var existingMembership = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (existingMembership != null)
            {
                if (existingMembership.IsActive)
                {
                    TempData["ErrorMessage"] = "این کاربر قبلاً عضو گروه است.";
                    return RedirectToAction(nameof(Members), new { id = groupId });
                }
                else
                {
                    // فعال‌سازی مجدد عضویت قبلی
                    existingMembership.IsActive = true;
                    existingMembership.IsAdmin = isAdmin;
                    existingMembership.Role = isAdmin ? GroupRole.Admin : GroupRole.Member;
                    existingMembership.JoinedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"کاربر «{user.FullName}» با موفقیت به گروه اضافه شد.";
                    return RedirectToAction(nameof(Members), new { id = groupId });
                }
            }

            // ایجاد عضویت جدید
            var userGroup = new UserGroup
            {
                UserId = userId,
                GroupId = groupId,
                IsAdmin = isAdmin,
                Role = isAdmin ? GroupRole.Admin : GroupRole.Member,
                JoinedAt = DateTime.Now,
                IsActive = true
            };

            _context.UserGroups.Add(userGroup);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"کاربر «{user.FullName}» با موفقیت به گروه اضافه شد.";
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // POST: Admin/Groups/AddMultipleMembers - افزودن چندین عضو همزمان
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMultipleMembers(int groupId, List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["ErrorMessage"] = "لطفاً حداقل یک کاربر انتخاب کنید.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            var group = await _context.Groups
                .Include(g => g.UserGroups.Where(ug => ug.IsActive))
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var currentMemberCount = group.UserGroups.Count(ug => ug.IsActive);
            var availableSlots = group.MaxMembers - currentMemberCount;

            if (userIds.Count > availableSlots)
            {
                TempData["ErrorMessage"] = $"تنها {availableSlots} ظرفیت خالی در گروه وجود دارد.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            var existingMemberIds = group.UserGroups
                .Where(ug => ug.IsActive)
                .Select(ug => ug.UserId)
                .ToList();

            var addedCount = 0;

            foreach (var userId in userIds)
            {
                if (existingMemberIds.Contains(userId))
                    continue;

                var existingInactive = await _context.UserGroups
                    .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId && !ug.IsActive);

                if (existingInactive != null)
                {
                    existingInactive.IsActive = true;
                    existingInactive.JoinedAt = DateTime.Now;
                }
                else
                {
                    var userGroup = new UserGroup
                    {
                        UserId = userId,
                        GroupId = groupId,
                        IsAdmin = false,
                        Role = GroupRole.Member,
                        JoinedAt = DateTime.Now,
                        IsActive = true
                    };
                    _context.UserGroups.Add(userGroup);
                }

                addedCount++;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{addedCount} کاربر با موفقیت به گروه اضافه شدند.";
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // POST: Admin/Groups/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int groupId, int userId)
        {
            var userGroup = await _context.UserGroups
                .Include(ug => ug.User)
                .Include(ug => ug.Group)
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup == null)
            {
                TempData["ErrorMessage"] = "عضویت یافت نشد.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            // جلوگیری از حذف Owner
            if (userGroup.Role == GroupRole.Owner)
            {
                TempData["ErrorMessage"] = "مالک گروه قابل حذف نیست. ابتدا مالکیت را به کاربر دیگری منتقل کنید.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            // حذف نرم
            userGroup.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"کاربر «{userGroup.User.FullName}» از گروه حذف شد.";
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // POST: Admin/Groups/ToggleAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(int groupId, int userId)
        {
            var userGroup = await _context.UserGroups
                .Include(ug => ug.User)
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId && ug.IsActive);

            if (userGroup == null)
            {
                TempData["ErrorMessage"] = "عضویت یافت نشد.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            // Owner قابل تغییر نیست
            if (userGroup.Role == GroupRole.Owner)
            {
                TempData["ErrorMessage"] = "نقش مالک گروه قابل تغییر نیست.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            userGroup.IsAdmin = !userGroup.IsAdmin;
            userGroup.Role = userGroup.IsAdmin ? GroupRole.Admin : GroupRole.Member;
            await _context.SaveChangesAsync();

            var message = userGroup.IsAdmin
                ? $"کاربر «{userGroup.User.FullName}» به ادمین ارتقا یافت."
                : $"دسترسی ادمین کاربر «{userGroup.User.FullName}» لغو شد.";

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // POST: Admin/Groups/TransferOwnership - انتقال مالکیت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferOwnership(int groupId, int newOwnerId)
        {
            var currentOwner = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.Role == GroupRole.Owner);

            var newOwnerMembership = await _context.UserGroups
                .Include(ug => ug.User)
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == newOwnerId && ug.IsActive);

            if (currentOwner == null || newOwnerMembership == null)
            {
                TempData["ErrorMessage"] = "خطا در انتقال مالکیت.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            // تنزل مالک قبلی به ادمین
            currentOwner.Role = GroupRole.Admin;
            currentOwner.IsAdmin = true;

            // ارتقای مالک جدید
            newOwnerMembership.Role = GroupRole.Owner;
            newOwnerMembership.IsAdmin = true;

            // به‌روزرسانی CreatorId در گروه
            var group = await _context.Groups.FindAsync(groupId);
            if (group != null)
            {
                group.CreatorId = newOwnerId;
                group.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"مالکیت گروه به «{newOwnerMembership.User.FullName}» منتقل شد.";
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // POST: Admin/Groups/MuteMember - بی‌صدا کردن عضو
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuteMember(int groupId, int userId, int? hours = null)
        {
            var userGroup = await _context.UserGroups
                .Include(ug => ug.User)
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId && ug.IsActive);

            if (userGroup == null)
            {
                TempData["ErrorMessage"] = "عضویت یافت نشد.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            userGroup.IsMuted = true;
            userGroup.MutedUntil = hours.HasValue ? DateTime.Now.AddHours(hours.Value) : null;
            await _context.SaveChangesAsync();

            var duration = hours.HasValue ? $"به مدت {hours} ساعت" : "بدون محدودیت زمانی";
            TempData["SuccessMessage"] = $"کاربر «{userGroup.User.FullName}» {duration} بی‌صدا شد.";
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // POST: Admin/Groups/UnmuteMember - رفع بی‌صدایی
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnmuteMember(int groupId, int userId)
        {
            var userGroup = await _context.UserGroups
                .Include(ug => ug.User)
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId && ug.IsActive);

            if (userGroup == null)
            {
                TempData["ErrorMessage"] = "عضویت یافت نشد.";
                return RedirectToAction(nameof(Members), new { id = groupId });
            }

            userGroup.IsMuted = false;
            userGroup.MutedUntil = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"بی‌صدایی کاربر «{userGroup.User.FullName}» رفع شد.";
            return RedirectToAction(nameof(Members), new { id = groupId });
        }

        // GET: Admin/Groups/Messages/5 - مشاهده پیام‌های گروه
        public async Task<IActionResult> Messages(int id, int page = 1, int pageSize = 50)
        {
            var group = await _context.Groups.FindAsync(id);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var query = _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == id && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt);

            var totalMessages = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalMessages / (double)pageSize);

            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MessageListViewModel
                {
                    Id = m.Id,
                    SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                    MessageText = m.MessageText ?? m.Content ?? "[بدون متن]",
                    SentAt = m.SentAt,
                    IsEdited = m.IsEdited,
                    Type = m.Type.ToString()
                })
                .ToListAsync();

            ViewBag.GroupId = id;
            ViewBag.GroupName = group.Name;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalMessages = totalMessages;

            return View(messages);
        }

        // POST: Admin/Groups/DeleteMessage - حذف پیام
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int groupId, int messageId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId);

            if (message == null)
            {
                TempData["ErrorMessage"] = "پیام یافت نشد.";
                return RedirectToAction(nameof(Messages), new { id = groupId });
            }

            // حذف نرم
            message.IsDeleted = true;
            message.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "پیام با موفقیت حذف شد.";
            return RedirectToAction(nameof(Messages), new { id = groupId });
        }

        // POST: Admin/Groups/Restore/5 - بازگردانی گروه حذف شده
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var group = await _context.Groups.FindAsync(id);

            if (group == null)
            {
                TempData["ErrorMessage"] = "گروه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            group.IsDeleted = false;
            group.IsActive = true;
            group.DeletedAt = null;
            group.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "گروه با موفقیت بازگردانی شد.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Groups/Deleted - لیست گروه‌های حذف شده
        public async Task<IActionResult> Deleted()
        {
            var deletedGroups = await _context.Groups
                .Include(g => g.Creator)
                .Where(g => g.IsDeleted)
                .OrderByDescending(g => g.DeletedAt)
                .Select(g => new GroupListViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CreatorName = g.Creator.FirstName + " " + g.Creator.LastName,
                    MemberCount = g.UserGroups.Count,
                    IsActive = g.IsActive,
                    IsPublic = g.IsPublic,
                    CreatedAt = g.CreatedAt,
                    DeletedAt = g.DeletedAt
                })
                .ToListAsync();

            return View(deletedGroups);
        }

        // GET: Admin/Groups/Export - خروجی Excel از لیست گروه‌ها
        public async Task<IActionResult> Export()
        {
            var groups = await _context.Groups
                .Include(g => g.Creator)
                .Include(g => g.UserGroups)
                .Where(g => !g.IsDeleted)
                .OrderBy(g => g.Name)
                .ToListAsync();

            // ساده‌ترین روش: خروجی CSV
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Name,Description,Creator,MemberCount,IsPublic,IsActive,CreatedAt");

            foreach (var g in groups)
            {
                var description = g.Description?.Replace(",", " ").Replace("\n", " ") ?? "";
                csv.AppendLine($"{g.Id},{g.Name},{description},{g.Creator.FullName},{g.UserGroups.Count(ug => ug.IsActive)},{g.IsPublic},{g.IsActive},{g.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Groups_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // متد کمکی برای بررسی وجود گروه
        private async Task<bool> GroupExistsAsync(int id)
        {
            return await _context.Groups.AnyAsync(e => e.Id == id);
        }
    }
}
