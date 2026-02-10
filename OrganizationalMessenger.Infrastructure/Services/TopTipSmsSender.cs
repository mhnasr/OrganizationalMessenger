using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Infrastructure.Services;

public class TopTipSmsSender : ISmsSender
{
    private readonly ApplicationDbContext _context;

    public TopTipSmsSender(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> SendSmsAsync(string messageBody, string phoneNumber, int? memberId = null, string smsType = "")
    {
        // ✅ تمیز کردن شماره
        phoneNumber = CleanPhoneNumber(phoneNumber);

        try
        {
            // ✅ خواندن تنظیمات از SystemSettings شما
            var apiKey = await GetSettingValueAsync("SmsApiKey");
            var senderNumber = await GetSettingValueAsync("SmsSenderNumber");

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderNumber))
            {
                return "تنظیمات TopTip کامل نیست";
            }

            // ✅ اضافه کردن "لغو 11"
            messageBody = EnsureUnsubscribeText(messageBody);

            // ✅ درخواست TopTip
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            string apiUrl = $"http://toptip.ir/webservice/rest/sms_send" +
                $"?note_arr[]={Uri.EscapeDataString(messageBody)}" +
                $"&api_key={apiKey}" +
                $"&receiver_number={phoneNumber}" +
                $"&sender_number={senderNumber}";

            var response = await httpClient.GetAsync(apiUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode &&
                   (responseContent.Contains("\"result\":true") || responseContent.Contains("\"result\": true"))
                ? "ارسال شد"
                : $"خطا: {responseContent}";
        }
        catch (Exception ex)
        {
            return $"خطا: {ex.Message}";
        }
    }

    private string CleanPhoneNumber(string phoneNumber)
    {
        return phoneNumber?.Trim()
            .Replace(" ", "").Replace("-", "")
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8").Replace("۹", "9") ?? "";
    }

    private string EnsureUnsubscribeText(string message)
    {
        return message + " | لغو 11";
    }

    private async Task<string?> GetSettingValueAsync(string key)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }
}
