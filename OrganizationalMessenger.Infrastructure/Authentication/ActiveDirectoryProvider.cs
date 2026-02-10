using Microsoft.Extensions.Configuration;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Domain.Entities;
using System.DirectoryServices.AccountManagement;

namespace OrganizationalMessenger.Infrastructure.Authentication
{
    public class ActiveDirectoryProvider : IAuthenticationProvider
    {
        private readonly IConfiguration _configuration;

        public ActiveDirectoryProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var domain = _configuration["Authentication:ActiveDirectory:Domain"];

                    using var context = new PrincipalContext(ContextType.Domain, domain);

                    bool isValid = context.ValidateCredentials(username, password);

                    if (!isValid)
                    {
                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "نام کاربری یا رمز عبور اشتباه است."
                        };
                    }

                    using var userPrincipal = UserPrincipal.FindByIdentity(context, username);

                    if (userPrincipal == null)
                    {
                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "کاربر یافت نشد."
                        };
                    }

                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        UserId = userPrincipal.SamAccountName,
                        FirstName = userPrincipal.GivenName,
                        LastName = userPrincipal.Surname,
                        Email = userPrincipal.EmailAddress,
                        PhoneNumber = userPrincipal.VoiceTelephoneNumber
                    };
                }
                catch (Exception ex)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"خطا در احراز هویت: {ex.Message}"
                    };
                }
            });
        }
    }
}
