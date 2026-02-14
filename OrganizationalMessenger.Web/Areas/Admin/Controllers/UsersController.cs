using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Web.Areas.Admin.Models;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    public class UsersController : BaseAdminController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UsersController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

       

        // لیست کاربران با قابلیت جستجو و صفحه‌بندی
        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 20)
        {
            var query = _context.Users.AsQueryable();

            // جستجو
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.Username.Contains(search) ||
                    u.PhoneNumber.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new UserListViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = search
            };

            return View(model);
        }

       



        // ایجاد کاربر جدید - GET
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        // ایجاد کاربر جدید - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // بررسی تکراری نبودن نام کاربری
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "این نام کاربری قبلاً استفاده شده است");
                return View(model);
            }

            // بررسی تکراری نبودن شماره موبایل
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "این شماره موبایل قبلاً ثبت شده است");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                ActiveDirectoryId = model.ActiveDirectoryId,
                ErpUserId = model.ErpUserId,
                IsActive = model.IsActive,
                CanCreateGroup = model.CanCreateGroup,
                CanCreateChannel = model.CanCreateChannel,
                CanMakeVoiceCall = model.CanMakeVoiceCall,
                CanMakeVideoCall = model.CanMakeVideoCall,
                SmsCredit = model.SmsCredit,
                CreatedAt = DateTime.Now,
                LastSeenAt = DateTime.Now
            };

            // ✅ ذخیره آواتار
            var avatarUrl = await SaveAvatarAsync(model.AvatarFile);
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
                user.ProfilePicture = avatarUrl; // اگر این فیلد را هم می‌خواهی هم‌سو نگه داری
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "کاربر با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }

        // ویرایش کاربر - GET
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var model = new UserEditViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                ActiveDirectoryId = user.ActiveDirectoryId,
                ErpUserId = user.ErpUserId,
                IsActive = user.IsActive,
                CanCreateGroup = user.CanCreateGroup,
                CanCreateChannel = user.CanCreateChannel,
                CanMakeVoiceCall = user.CanMakeVoiceCall,
                CanMakeVideoCall = user.CanMakeVideoCall,
                SmsCredit = user.SmsCredit,
                AvatarUrl = user.AvatarUrl // ✅ برای نمایش در فرم
            };

            return View(model);
        }


        // ویرایش کاربر - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            // چک‌های تکراری قبلی...

            user.Username = model.Username;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;
            user.ActiveDirectoryId = model.ActiveDirectoryId;
            user.ErpUserId = model.ErpUserId;
            user.IsActive = model.IsActive;
            user.CanCreateGroup = model.CanCreateGroup;
            user.CanCreateChannel = model.CanCreateChannel;
            user.CanMakeVoiceCall = model.CanMakeVoiceCall;
            user.CanMakeVideoCall = model.CanMakeVideoCall;
            user.SmsCredit = model.SmsCredit;

            // ✅ اگر فایل جدید آمده، ذخیره و جایگزین کن
            var newAvatarUrl = await SaveAvatarAsync(model.AvatarFile);
            if (!string.IsNullOrEmpty(newAvatarUrl))
            {
                user.AvatarUrl = newAvatarUrl;
                user.ProfilePicture = newAvatarUrl;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "اطلاعات کاربر با موفقیت ویرایش شد";
            return RedirectToAction(nameof(Index));
        }


        // حذف کاربر
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "کاربر با موفقیت حذف شد";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "به دلیل وجود داده‌های مرتبط، امکان حذف این کاربر وجود ندارد";
            }

            return RedirectToAction(nameof(Index));
        }

        // فعال/غیرفعال کردن کاربر
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = user.IsActive });
        }




        private async Task<string?> SaveAvatarAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // مسیر قابل استفاده در HTML
            return $"/uploads/avatars/{fileName}";
        }


    }
}
