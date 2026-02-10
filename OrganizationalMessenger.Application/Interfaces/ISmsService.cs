namespace OrganizationalMessenger.Application.Interfaces
{
    public interface ISmsService
    {
        Task<SmsResult> SendOtpAsync(string phoneNumber, string otpCode);
        Task<SmsResult> SendMessageAsync(string phoneNumber, string message);
    }

    public class SmsResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? MessageId { get; set; }
    }
}
