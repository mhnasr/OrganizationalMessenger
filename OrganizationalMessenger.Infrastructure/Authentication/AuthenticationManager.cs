using Microsoft.Extensions.DependencyInjection;
using OrganizationalMessenger.Application.Interfaces;

namespace OrganizationalMessenger.Infrastructure.Authentication
{
    public class AuthenticationManager : IAuthenticationManager
    {
        private readonly IServiceProvider _serviceProvider;

        public AuthenticationManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(
            string username,
            string password,
            AuthenticationType type)
        {
            var provider = GetProvider(type);
            return await provider.AuthenticateAsync(username, password);
        }

        public async Task<(bool Success, string Message)> SendOtpAsync(string phoneNumber)
        {
            var otpProvider = _serviceProvider.GetRequiredService<OtpAuthenticationProvider>();
            return await otpProvider.SendOtpAsync(phoneNumber);
        }

        public IAuthenticationProvider GetProvider(AuthenticationType type)
        {
            return type switch
            {
                AuthenticationType.ActiveDirectory =>
                    _serviceProvider.GetRequiredService<ActiveDirectoryProvider>(),

                AuthenticationType.ERP =>
                    _serviceProvider.GetRequiredService<ErpAuthenticationProvider>(),

                AuthenticationType.SMS =>
                    _serviceProvider.GetRequiredService<OtpAuthenticationProvider>(),

                _ => throw new NotSupportedException($"Authentication type {type} is not supported.")
            };
        }
    }
}
