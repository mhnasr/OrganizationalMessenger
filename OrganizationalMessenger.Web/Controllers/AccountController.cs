using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Authentication;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Infrastructure.Services;
using System.Security.Claims;

namespace OrganizationalMessenger.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthenticationManager _authManager;
        private readonly ISmsSender _smsSender;

        public AccountController(
            ApplicationDbContext context,
            IAuthenticationManager authManager,
            ISmsSender smsSender)
        {
            _context = context;
            _authManager = authManager;
            _smsSender = smsSender;
        }

        // ==================== صفحه لاگین ====================
        [HttpGet]
        public IActionResult Login()
        {
            // اگر کاربر لاگین است، به صفحه Chat برود
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Chat");
            }

            return View();
        }

        // ==================== درخواست کد OTP ====================
        [HttpPost]
        public async Task<IActionResult> SendOtp(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return Json(new { success = false, message = "شماره موبایل الزامی است" });
                }

                // پاکسازی شماره
                phoneNumber = CleanPhoneNumber(phoneNumber);

                // بررسی فرمت شماره
                if (!phoneNumber.StartsWith("09") || phoneNumber.Length != 11)
                {
                    return Json(new { success = false, message = "فرمت شماره موبایل درست نیست" });
                }

                // بررسی کاربر در دیتابیس
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    return Json(new { success = false, message = "این شماره موبایل در سیستم ثبت نشده است" });
                }


                var success = await _smsSender.SendSmsAsync(
                    $"کد تایید: {GenerateOtp()}",
                    phoneNumber
                );

                //if (!success)
                //{
                //    return Json(new { success = false, message = "خطا در ا��سال پیامک" });
                //}

                // ذخیره OTP در Session (تنها برای توسعه)
                HttpContext.Session.SetString("OTP_Phone", phoneNumber);

                return Json(new { success = true, message = "کد تایید ارسال شد" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا: {ex.Message}" });
            }
        }

        // ==================== تایید OTP ====================
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string phoneNumber, string otpCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(otpCode))
                {
                    TempData["Error"] = "شماره موبایل و کد تایید الزامی است";
                    return RedirectToAction(nameof(Login));
                }

                // پاکسازی شماره
                phoneNumber = CleanPhoneNumber(phoneNumber);

                // بررسی OTP (در محیط واقعی از سرویس پیامک استفاده کنید)
                var sessionOtp = HttpContext.Session.GetString("OTP_Phone");
                if (sessionOtp != phoneNumber)
                {
                    TempData["Error"] = "کد تایید صحیح نیست";
                    return RedirectToAction(nameof(Login));
                }

                // پیدا کردن کاربر
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    TempData["Error"] = "کاربر یافت نشد";
                    return RedirectToAction(nameof(Login));
                }

                // ورود کاربر
                await SignInUserAsync(user);

                // بروزرسانی آخرین زمان آنلاین
                user.LastSeenAt = DateTime.Now;
                user.IsOnline = true;
                await _context.SaveChangesAsync();

                // پاک کردن OTP از Session
                HttpContext.Session.Remove("OTP_Phone");

                return RedirectToAction("Index", "Chat");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا: {ex.Message}";
                return RedirectToAction(nameof(Login));
            }
        }

        // ==================== ورود با رمز عبور ====================
        [HttpPost]
        public async Task<IActionResult> LoginWithPassword(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["Error"] = "نام کاربری و رمز عبور الزامی است";
                    return RedirectToAction(nameof(Login));
                }

                // پاکسازی نام کاربری (شماره موبایل)
                username = CleanPhoneNumber(username);

                // پیدا کردن کاربر
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == username && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است";
                    return RedirectToAction(nameof(Login));
                }

                // بررسی رمز عبور (استفاده از BCrypt یا دیگر روش)
                // در اینجا فرض کردیم که BCrypt استفاده می‌شود
                if (user.PasswordHash == null || !VerifyPassword(password, user.PasswordHash))
                {
                    TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است";
                    return RedirectToAction(nameof(Login));
                }

                // ورود کاربر
                await SignInUserAsync(user);

                // بروزرسانی آخرین زمان آنلاین
                user.LastSeenAt = DateTime.Now;
                user.IsOnline = true;
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Chat");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا: {ex.Message}";
                return RedirectToAction(nameof(Login));
            }
        }

        // ==================== ورود با Active Directory ====================
        [HttpPost]
        public async Task<IActionResult> LoginWithAD(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["Error"] = "نام کاربری و رمز عبور الزامی است";
                    return RedirectToAction(nameof(Login));
                }

                // احراز هویت از طریق AD
                var authResult = await _authManager.AuthenticateAsync(
                    username,
                    password,
                    AuthenticationType.ActiveDirectory
                );

                if (!authResult.IsSuccess)
                {
                    TempData["Error"] = authResult.ErrorMessage ?? "احراز هوی�� ناموفق";
                    return RedirectToAction(nameof(Login));
                }

                // پیدا کردن یا ایجاد کاربر
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ActiveDirectoryId == authResult.UserId && u.IsActive);

                if (user == null)
                {
                    // ایجاد کاربر جدید
                    user = new User
                    {
                        Username = authResult.UserId,
                        FirstName = authResult.FirstName ?? "",
                        LastName = authResult.LastName ?? "",
                        Email = authResult.Email,
                        PhoneNumber = authResult.PhoneNumber,
                        ActiveDirectoryId = authResult.UserId,
                        IsADAuthenticated = true,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        SmsCredit = 10
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // ورود کاربر
                await SignInUserAsync(user);

                // بروزرسانی آخرین زمان آنلاین
                user.LastSeenAt = DateTime.Now;
                user.IsOnline = true;
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Chat");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا: {ex.Message}";
                return RedirectToAction(nameof(Login));
            }
        }

        // ==================== خروج ====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // بروزرسانی وضعیت آنلاین
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out int id))
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user != null)
                    {
                        user.IsOnline = false;
                        user.LastSeenAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch { }

            return RedirectToAction(nameof(Login));
        }

        // ==================== توابع کمکی ====================

        /// <summary>
        /// پاکسازی شماره موبایل
        /// </summary>
        private string CleanPhoneNumber(string phone)
        {
            return phone?.Trim()
                .Replace(" ", "").Replace("-", "")
                .Replace("۰", "0").Replace("۱", "1")
                .Replace("۲", "2").Replace("۳", "3")
                .Replace("۴", "4").Replace("۵", "5")
                .Replace("۶", "6").Replace("۷", "7")
                .Replace("۸", "8").Replace("۹", "9") ?? "";
        }

        /// <summary>
        /// تولید کد OTP 6 رقمی
        /// </summary>
        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// بررسی رمز عبور (BCrypt)
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ورود کاربر به سیستم
        /// </summary>
        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                new Claim("PhoneNumber", user.PhoneNumber ?? ""),
                new Claim("Avatar", user.AvatarUrl ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
        }



        

    }
}