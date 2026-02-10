using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Web.Areas.Admin.Models;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("AdminId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.Username == model.Username && a.IsActive);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(model.Password, admin.PasswordHash))
            {
                ModelState.AddModelError("", "نام کاربری یا رمز عبور اشتباه است");
                return View(model);
            }

            // به‌روزرسانی آخرین ورود
            admin.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // ✅ ذخیره در Session - همه به صورت یکپارچه
            HttpContext.Session.SetInt32("AdminId", (int)admin.Id);  // ✅ تغییر به SetInt32
            HttpContext.Session.SetString("AdminUsername", admin.Username);
            HttpContext.Session.SetString("AdminFullName", $"{admin.FirstName} {admin.LastName}");

            // اگر IsSuperAdmin دارید
           // HttpContext.Session.SetInt32("IsSuperAdmin", admin.IsSuperAdmin ? 1 : 0);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
