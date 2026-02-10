using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace OrganizationalMessenger.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class OtpAuthenticationController : ControllerBase
    {
        private readonly IAuthenticationManager _authManager;
        private readonly ApplicationDbContext _context;

        public OtpAuthenticationController(IAuthenticationManager authManager, ApplicationDbContext context)
        {
            _authManager = authManager;
            _context = context;
        }

        /// <summary>
        /// مرحله 1: درخواست OTP
        /// POST: api/otpauthentication/send-otp
        /// </summary>
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "شماره موبایل معتبر نیست" });
            }

            // چک کردن اینکه کاربر وجود دارد یا نه
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
            {
                return NotFound(new { success = false, message = "کاربری با این شماره موبایل یافت نشد" });
            }

            if (!user.IsActive)
            {
                return BadRequest(new { success = false, message = "این کاربر غیرفعال است" });
            }

            // ارسال OTP
            var (success, message) = await _authManager.SendOtpAsync(request.PhoneNumber);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message = "کد تایید برای شما ارسال شد",
                phoneNumber = request.PhoneNumber
            });
        }

        /// <summary>
        /// مرحله 2: تایید OTP و لاگین
        /// POST: api/otpauthentication/verify-otp
        /// </summary>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "اطلاعات معتبر نیست" });
            }

            var result = await _authManager.AuthenticateAsync(request.PhoneNumber, request.OtpCode, AuthenticationType.SMS);

            if (!result.IsSuccess)
            {
                return Unauthorized(new { success = false, message = result.ErrorMessage });
            }

            // ایجاد Claims و لاگین
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId),
                new Claim(ClaimTypes.Name, result.FirstName + " " + result.LastName),
                new Claim("PhoneNumber", result.PhoneNumber ?? ""),
                new Claim("Email", result.Email ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return Ok(new
            {
                success = true,
                message = "ورود موفق",
                user = new
                {
                    id = result.UserId,
                    fullName = result.FirstName + " " + result.LastName,
                    phoneNumber = result.PhoneNumber,
                    email = result.Email
                }
            });
        }

        /// <summary>
        /// لاگین با نام کاربری و رمز عبور (اختیاری)
        /// POST: api/otpauthentication/login-password
        /// </summary>
        [HttpPost("login-password")]
        public async Task<IActionResult> LoginWithPassword([FromBody] LoginPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "اطلاعات معتبر نیست" });
            }

            // شماره موبایل به عنوان username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.Username);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return Unauthorized(new { success = false, message = "نام کاربری یا رمز عبور اشتب��ه است" });
            }

            // بررسی رمز عبور
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new { success = false, message = "نام کاربری یا رمز عبور اشتباه است" });
            }

            if (!user.IsActive)
            {
                return BadRequest(new { success = false, message = "این کاربر غیرفعال است" });
            }

            // لاگین
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim("PhoneNumber", user.PhoneNumber ?? ""),
                new Claim("Email", user.Email ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return Ok(new
            {
                success = true,
                message = "ورود موفق",
                user = new
                {
                    id = user.Id,
                    fullName = user.FirstName + " " + user.LastName,
                    phoneNumber = user.PhoneNumber,
                    email = user.Email
                }
            });
        }

        /// <summary>
        /// چک کردن اینکه کاربر رمز عبور تعریف کرده یا نه
        /// GET: api/otpauthentication/has-password/{phoneNumber}
        /// </summary>
        [HttpGet("has-password/{phoneNumber}")]
        public async Task<IActionResult> HasPassword(string phoneNumber)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                return NotFound(new { success = false, message = "کاربری یافت نشد" });
            }

            return Ok(new
            {
                success = true,
                hasPassword = !string.IsNullOrEmpty(user.PasswordHash)
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { success = true, message = "خروج موفق" });
        }
    }

    // ==================== Request Models ====================

    public class SendOtpRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [Phone(ErrorMessage = "فرمت شماره موبایل صحیح نیست")]
        [Display(Name = "شماره موبایل")]
        public string PhoneNumber { get; set; }
    }

    public class VerifyOtpRequest
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "کد تایید الزامی است")]
        [StringLength(6, MinimumLength = 4)]
        public string OtpCode { get; set; }
    }

    public class LoginPasswordRequest
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        public string Username { get; set; } // شماره موبایل

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        public string Password { get; set; }
    }
}