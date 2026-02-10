using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Infrastructure.Services;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Claims;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly OtpService _otpService;

    public AccountController(ApplicationDbContext context, OtpService otpService)
    {
        _context = context;
        _otpService = otpService;
    }

    public IActionResult Login()
    {
        return View();
    }

    // ==================== تولید OTP ====================
    [HttpPost]
    public async Task<IActionResult> GenerateOtp(string phoneNumber)
    {
        var (success, otpCode, message) = await _otpService.GenerateOtpAsync(phoneNumber);

        return Json(new { success, otpCode, message });
    }

    // ==================== تایید OTP ====================
    [HttpPost]
    public async Task<IActionResult> VerifyOtp(string phoneNumber, string otpCode)
    {
        var (success, message) = await _otpService.VerifyOtpAsync(phoneNumber, otpCode);

        if (!success)
        {
            TempData["Error"] = message;
            return RedirectToAction("Login");
        }

        // ورود کاربر
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
            TempData["Error"] = "کاربر یافت نشد";
            return RedirectToAction("Login");
        }

        await SignInUserAsync(user);
        return RedirectToAction("Index", "Chat");
    }

    // ==================== ورود با پسورد ====================
    [HttpPost]
    public async Task<IActionResult> LoginWithPassword(string username, string password)
    {
        // تست
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            TempData["Error"] = "کاربر یافت نشد";
            return RedirectToAction("Login");
        }

        await SignInUserAsync(user);
        return RedirectToAction("Index", "Chat");
    }

    private async Task SignInUserAsync(OrganizationalMessenger.Domain.Entities.User user)
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

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}