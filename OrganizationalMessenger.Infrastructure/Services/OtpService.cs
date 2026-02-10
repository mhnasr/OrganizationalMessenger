using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Infrastructure.Data;

namespace OrganizationalMessenger.Infrastructure.Services
{
    public class OtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private static readonly Dictionary<string, (string Code, DateTime Expiry)> _otpCache = new();

        public OtpService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<(bool Success, string OtpCode, string Message)> GenerateOtpAsync(string phoneNumber)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
                if (user == null)
                    return (false, "", "کاربر با این شماره موبایل یافت نشد");

                // ✅ تولید OTP 6 رقمی
                var otpCode = new Random().Next(100000, 999999).ToString();
                var expiry = DateTime.UtcNow.AddMinutes(5);

                // ✅ ذخیره در کش (برای تست)
                _otpCache[phoneNumber] = (otpCode, expiry);

                // 🔥 DEBUG: نمایش OTP
                System.Diagnostics.Debug.WriteLine($"🔥 OTP برای {phoneNumber}: {otpCode}");

                return (true, otpCode, "کد تایید با موفقیت ارسال شد");
            }
            catch (Exception ex)
            {
                return (false, "", $"خطا: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            if (!_otpCache.TryGetValue(phoneNumber, out var cached))
                return (false, "کد تایید یافت نشد");

            if (cached.Expiry < DateTime.UtcNow)
            {
                _otpCache.Remove(phoneNumber);
                return (false, "کد تایید منقضی شده است");
            }

            if (cached.Code != otpCode)
                return (false, "کد تایید اشتباه است");

            _otpCache.Remove(phoneNumber);
            return (true, "تایید با موفقیت انجام شد");
        }
    }
}