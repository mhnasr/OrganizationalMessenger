using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Infrastructure.Data;
using System.Security.Claims;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthenticationManager _authManager;

    public AccountController(ApplicationDbContext context, IAuthenticationManager authManager)
    {
        _context = context;
        _authManager = authManager;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // تست کاربران ساده
        var user = _context.Users.FirstOrDefault(u => u.Username == username &&
            u.PasswordHash == "AQAAAAEAACcQAAAAEG3LzixU/DMivW0V8ALZr0eH5x4oJvVDIfQiGaPYTB141YiQCDY5ale+wjF3R0C8Q==");

        if (user != null && user.IsActive)
        {
            // ✅ Cookie Login
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("FullName", user.FirstName + " " + user.LastName),
                new Claim("PhoneNumber", user.PhoneNumber ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Chat");
        }

        ViewBag.Error = "نام کاربری یا رمز اشتباه";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
