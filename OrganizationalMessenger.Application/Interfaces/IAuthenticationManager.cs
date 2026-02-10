namespace OrganizationalMessenger.Application.Interfaces
{
    public interface IAuthenticationManager
    {
        Task<AuthenticationResult> AuthenticateAsync(string username, string password, AuthenticationType type);
        Task<(bool Success, string Message)> SendOtpAsync(string phoneNumber);
        IAuthenticationProvider GetProvider(AuthenticationType type);
    }

    public enum AuthenticationType
    {
        ActiveDirectory,
        ERP,
        OTP
    }
}
