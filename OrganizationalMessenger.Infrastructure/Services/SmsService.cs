using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using OrganizationalMessenger.Application.Interfaces;

namespace OrganizationalMessenger.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _sender;
        private readonly string _apiUrl;

        public SmsService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;

            // تنظیمات پنل SMS (مثلاً کاوه‌نگار، ملی‌پیامک و...)
            _apiKey = _configuration["Sms:ApiKey"] ?? "";
            _sender = _configuration["Sms:Sender"] ?? "";
            _apiUrl = _configuration["Sms:ApiUrl"] ?? "";
        }

        public async Task<SmsResult> SendOtpAsync(string phoneNumber, string otpCode)
        {
            var message = $"کد تایید شما: {otpCode}\nاین کد تا 5 دقیقه معتبر است.";
            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<SmsResult> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                // مثال برای Kavenegar API
                var request = new
                {
                    sender = _sender,
                    receptor = phoneNumber,
                    message = message
                };

                _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiUrl}/v1/{_apiKey}/sms/send.json",
                    request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SmsApiResponse>();

                    return new SmsResult
                    {
                        Success = result?.Return?.Status == 200,
                        Message = "پیامک با موفقیت ارسال شد",
                        MessageId = result?.Entries?.FirstOrDefault()?.Messageid.ToString()
                    };
                }

                return new SmsResult
                {
                    Success = false,
                    Message = "خطا در ارسال پیامک"
                };
            }
            catch (Exception ex)
            {
                return new SmsResult
                {
                    Success = false,
                    Message = $"خطا: {ex.Message}"
                };
            }
        }

        private class SmsApiResponse
        {
            public ReturnData? Return { get; set; }
            public List<EntryData>? Entries { get; set; }
        }

        private class ReturnData
        {
            public int Status { get; set; }
            public string? Message { get; set; }
        }

        private class EntryData
        {
            public long Messageid { get; set; }
            public string? Message { get; set; }
            public int Status { get; set; }
            public string? Statustext { get; set; }
            public string? Sender { get; set; }
            public string? Receptor { get; set; }
            public int Date { get; set; }
            public int Cost { get; set; }
        }
    }
}
