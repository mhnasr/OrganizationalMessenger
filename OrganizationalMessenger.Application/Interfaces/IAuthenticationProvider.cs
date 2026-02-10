namespace OrganizationalMessenger.Application.Interfaces
{
    public interface IAuthenticationProvider
    {
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    }

    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
