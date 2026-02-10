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
        Database = 0,      // شماره موبایل از دیتابیس
        ActiveDirectory = 1,
        ERP = 2,
        SMS = 3           // اضافه شد - OTP پیامکی
    }
}
