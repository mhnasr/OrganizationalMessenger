namespace OrganizationalMessenger.Infrastructure.Services
{
    public interface ISmsSender
    {
        Task<SmsSendResult> SendSmsAsync(string messageBody, string phoneNumber, int? memberId = null, string smsType = "");
    }

    public class SmsSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? MessageId { get; set; }
    }
}