using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Infrastructure.Data;

namespace OrganizationalMessenger.Infrastructure.Authentication
{
    public class OtpAuthenticationProvider : IAuthenticationProvider
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private static readonly Dictionary<string, (string Code, DateTime Expiry)> _otpCache = new();

        public OtpAuthenticationProvider(
            ApplicationDbContext context,
            ISmsService smsService)
        {
            _context = context;
            _smsService = smsService;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string phoneNumber, string otpCode)
        {
            if (!_otpCache.TryGetValue(phoneNumber, out var cachedOtp))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "کد تایید یافت نشد. لطفاً دوباره درخواست دهید."
                };
            }

            if (cachedOtp.Expiry < DateTime.UtcNow)
            {
                _otpCache.Remove(phoneNumber);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "کد تایید منقضی شده است."
                };
            }

            if (cachedOtp.Code != otpCode)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "کد تایید اشتباه است."
                };
            }

            _otpCache.Remove(phoneNumber);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "کاربر با این شماره تلفن یافت نشد."
                };
            }

            return new AuthenticationResult
            {
                IsSuccess = true,
                UserId = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<(bool Success, string Message)> SendOtpAsync(string phoneNumber)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                return (false, "کاربر با این شماره تلفن یافت نشد.");
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            _otpCache[phoneNumber] = (otpCode, expiry);

            var smsResult = await _smsService.SendOtpAsync(phoneNumber, otpCode);

            return (smsResult.Success, smsResult.Message);
        }
    }
}
