// Areas/Admin/Controllers/SettingsController.cs - نسخه FINAL بدون ViewModel
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Web.Areas.Admin.Models;
using System.Text.Json;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<string?> GetSettingValueAsync(string key)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        private async Task SaveSettingAsync(string key, string? value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                _context.SystemSettings.Add(new SystemSetting { Key = key, Value = value ?? "", UpdatedAt = DateTime.Now });
            }
            else
            {
                setting.Value = value ?? "";
                setting.UpdatedAt = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToListAsync();

            ViewBag.TotalSettings = settings.Count;

            // ✅ Safe DateTime
            var lastUpdated = await _context.SystemSettings
                .MaxAsync(s => (DateTime?)s.UpdatedAt) ?? DateTime.Now;
            ViewBag.LastUpdated = lastUpdated; // ✅ non-null

            return View(settings);
        }


        #region General


        [HttpGet]
        public async Task<IActionResult> General()
        {
            ViewBag.OrganizationName = await GetSettingValueAsync("CompanyName") ?? "سازمان";
            ViewBag.LogoUrl = await GetSettingValueAsync("CompanyLogoUrl");

            // ✅ دقیقاً "true" چک کنید (نه فقط true)
            var groupSetting = await GetSettingValueAsync("AllowUserGroupCreation");
            ViewBag.AllowUserGroupCreation = groupSetting == "true";

            var channelSetting = await GetSettingValueAsync("AllowUserChannelCreation");
            ViewBag.AllowUserChannelCreation = channelSetting == "true";

            ViewBag.MaxGroupMembers = await GetSettingValueAsync("MaxGroupMembers") ?? "200";

            return View();
        }




        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> General(string organizationName, IFormFile? logoFile,
     bool allowUserGroupCreation, bool allowUserChannelCreation, int maxGroupMembers)
        {
            try
            {
                // ذخیره نام سازمان
                await SaveSettingAsync("CompanyName", organizationName);
                await SaveSettingAsync("AllowUserGroupCreation", allowUserGroupCreation.ToString().ToLower());
                await SaveSettingAsync("AllowUserChannelCreation", allowUserChannelCreation.ToString().ToLower());
                await SaveSettingAsync("MaxGroupMembers", maxGroupMembers.ToString());

                // آپلود لوگو
                if (logoFile != null && logoFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
                    Directory.CreateDirectory(uploadsPath);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(logoFile.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await logoFile.CopyToAsync(stream);
                    }

                    await SaveSettingAsync("CompanyLogoUrl", $"/uploads/logos/{fileName}");
                }

                TempData["Success"] = "تنظیمات با موفقیت ذخیره شد";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در ذخیره: " + ex.Message;
            }
            return RedirectToAction(nameof(General));
        }



        #endregion

        #region Authentication
        [HttpGet]
        public async Task<IActionResult> Authentication()
        {
            ViewBag.AuthenticationType = await GetSettingValueAsync("AuthenticationType") ?? "Database";
            ViewBag.AdServer = await GetSettingValueAsync("AdServer") ?? "";
            ViewBag.AdDomain = await GetSettingValueAsync("AdDomain") ?? "";
            ViewBag.ErpApiUrl = await GetSettingValueAsync("ErpApiUrl") ?? "";
            ViewBag.OtpExpiryMinutes = await GetSettingValueAsync("OtpExpiryMinutes") ?? "5";
            ViewBag.OtpEnabled = await GetSettingValueAsync("OtpEnabled") == "true";

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Authentication(
            string authenticationType, string adServer, string adDomain, string erpApiUrl,
            bool otpEnabled, int otpExpiryMinutes)
        {
            // 🔥 DEBUG - در Output Window ببینید
            System.Diagnostics.Debug.WriteLine($"DEBUG: Type={authenticationType}, OTP={otpEnabled}");

            await SaveSettingAsync("AuthenticationType", authenticationType);
            await SaveSettingAsync("AdServer", adServer);
            await SaveSettingAsync("AdDomain", adDomain);
            await SaveSettingAsync("ErpApiUrl", erpApiUrl);
            await SaveSettingAsync("OtpEnabled", otpEnabled.ToString().ToLower());
            await SaveSettingAsync("OtpExpiryMinutes", otpExpiryMinutes.ToString());

            TempData["Success"] = $"ذخیره شد: {authenticationType}, OTP={otpEnabled}";
            return RedirectToAction(nameof(Authentication));
        }


        #endregion

        #region FileUpload
        public async Task<IActionResult> FileUpload()
        {
            var settings = await _context.FileUploadSettings.ToListAsync();
            return View(settings);
        }

        // ✅ UPDATE (اصلاح موجود)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFileType(int id, long maxSize, bool isAllowed)
        {
            var setting = await _context.FileUploadSettings.FindAsync(id);
            if (setting != null)
            {
                setting.MaxSize = maxSize;
                setting.IsAllowed = isAllowed;
                await _context.SaveChangesAsync();
                TempData["Success"] = "فایل بروزرسانی شد";
            }
            return RedirectToAction(nameof(FileUpload));
        }

        // ✅ CREATE (اضافه جدید)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFileType(string fileType, string category, long maxSize, bool isAllowed)
        {
            if (!string.IsNullOrEmpty(fileType))
            {
                // چک تکراری
                var exists = await _context.FileUploadSettings.AnyAsync(x => x.FileType == fileType);
                if (!exists)
                {
                    _context.FileUploadSettings.Add(new FileUploadSetting
                    {
                        FileType = fileType.ToLower(),
                        Category = category,
                        MaxSize = maxSize,
                        IsAllowed = isAllowed
                    });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "فایل اضافه شد";
                }
                else
                {
                    TempData["Error"] = "این نوع فایل قبلاً وجود دارد";
                }
            }
            return RedirectToAction(nameof(FileUpload));
        }

        // ✅ DELETE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFileType(int id)
        {
            var setting = await _context.FileUploadSettings.FindAsync(id);
            if (setting != null)
            {
                _context.FileUploadSettings.Remove(setting);
                await _context.SaveChangesAsync();
                TempData["Success"] = "فایل حذف شد";
            }
            return RedirectToAction(nameof(FileUpload));
        }
        #endregion


        #region VoIP

        [HttpGet]
        public async Task<IActionResult> VoIP()
        {
            ViewBag.VoipEnabled = await GetSettingValueAsync("VoipEnabled") == "true";
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VoIP(bool voipEnabled)
        {
            // 🔥 DEBUG
            System.Diagnostics.Debug.WriteLine($"🔥 VoIP Debug: {voipEnabled}");

            await SaveSettingAsync("VoipEnabled", voipEnabled.ToString().ToLower());

            // ✅ TempData["Success"] (نه SuccessMessage)
            TempData["Success"] = $"VoIP {(voipEnabled ? "فعال" : "غیرفعال")} شد";
            return RedirectToAction(nameof(VoIP));
        }


        #endregion

        #region Telegram
        [HttpGet]
        public async Task<IActionResult> Telegram()
        {
            ViewBag.TelegramEnabled = await GetSettingValueAsync("TelegramIntegrationEnabled") == "true";
            ViewBag.BotToken = await GetSettingValueAsync("TelegramBotToken") ?? "";
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Telegram(bool telegramEnabled, string? botToken)
        {
            // 🔥 DEBUG
            System.Diagnostics.Debug.WriteLine($"🔥 Telegram Debug: Enabled={telegramEnabled}, Token={botToken?.Substring(0, 10)}...");

            await SaveSettingAsync("TelegramIntegrationEnabled", telegramEnabled.ToString().ToLower());
            await SaveSettingAsync("TelegramBotToken", botToken);

            TempData["Success"] = $"تلگرام {(telegramEnabled ? "فعال" : "غیرفعال")} شد";
            return RedirectToAction(nameof(Telegram));
        }
        #endregion



        #region message
        [HttpGet]
        public async Task<IActionResult> MessageSettings()
        {
            ViewBag.MessageEditTimeLimit = await GetSettingValueAsync("MessageEditTimeLimit") ?? "3600";
            ViewBag.MessageDeleteTimeLimit = await GetSettingValueAsync("MessageDeleteTimeLimit") ?? "7200";
            ViewBag.AllowMessageEdit = await GetSettingValueAsync("AllowMessageEdit") == "true";
            ViewBag.AllowMessageDelete = await GetSettingValueAsync("AllowMessageDelete") == "true";

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MessageSettings(
            int messageEditTimeLimit, int messageDeleteTimeLimit,
            bool allowMessageEdit, bool allowMessageDelete)
        {
            // 🔥 DEBUG
            System.Diagnostics.Debug.WriteLine($"EditTime={messageEditTimeLimit}, Edit={allowMessageEdit}");

            await SaveSettingAsync("MessageEditTimeLimit", messageEditTimeLimit.ToString());
            await SaveSettingAsync("MessageDeleteTimeLimit", messageDeleteTimeLimit.ToString());
            await SaveSettingAsync("AllowMessageEdit", allowMessageEdit.ToString().ToLower());
            await SaveSettingAsync("AllowMessageDelete", allowMessageDelete.ToString().ToLower());

            TempData["Success"] = $"ذخیره شد: ویرایش={allowMessageEdit}, حذف={allowMessageDelete}";
            return RedirectToAction(nameof(MessageSettings));
        }



        #endregion

        #region ForbiddenWords
        public async Task<IActionResult> ForbiddenWords()
        {
            var words = await _context.ForbiddenWords.ToListAsync();
            return View(words);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddForbiddenWord(string newWord)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(newWord))
                {
                    // ✅ چک تکراری قبل از Add
                    var trimmedWord = newWord.Trim().ToLower();
                    var exists = await _context.ForbiddenWords.AnyAsync(w => w.Word.ToLower() == trimmedWord);

                    if (exists)
                    {
                        TempData["Error"] = $"کلمه '{newWord.Trim()}' قبلاً وجود دارد";
                        return RedirectToAction(nameof(ForbiddenWords));
                    }

                    // ✅ اضافه کردن
                    _context.ForbiddenWords.Add(new ForbiddenWord
                    {
                        Word = trimmedWord,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"کلمه '{newWord.Trim()}' اضافه شد";
                }
                else
                {
                    TempData["Error"] = "کلمه نمی‌تواند خالی باشد";
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                // ✅ Catch خطای Unique Constraint
                TempData["Error"] = "این کلمه قبلاً وجود دارد";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در ذخیره: " + ex.Message;
            }

            return RedirectToAction(nameof(ForbiddenWords));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForbiddenWord(int id)
        {
            try
            {
                var word = await _context.ForbiddenWords.FindAsync(id);
                if (word != null)
                {
                    _context.ForbiddenWords.Remove(word);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"کلمه '{word.Word}' حذف شد";
                }
                else
                {
                    TempData["Error"] = "کلمه پیدا نشد";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در حذف: " + ex.Message;
            }
            return RedirectToAction(nameof(ForbiddenWords));
        }
        #endregion


        #region ServiceLinks
        public async Task<IActionResult> ServiceLinks()
        {
            var links = await _context.ServiceLinks.ToListAsync();
            return View(links);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddServiceLink(string title, string url, string? iconUrl = null)
        {
            try
            {
                // ✅ حل Foreign Key - AdminUser فعلی را پیدا کن
                var currentAdminId = GetCurrentAdminId(); // یا از User.Identity استفاده کنید

                var link = new ServiceLink
                {
                    Title = title,
                    Url = url,
                    IconUrl = iconUrl,
                    DisplayOrder = await GetNextDisplayOrderAsync(),
                    CreatedByAdminId = currentAdminId,  // ✅ Foreign Key
                    CreatedAt = DateTime.Now
                };

                _context.ServiceLinks.Add(link);
                await _context.SaveChangesAsync();

                TempData["Success"] = "لینک با موفقیت اضافه شد";
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
            {
                TempData["Error"] = "خطا در ذخیره: AdminUser پیدا نشد";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا: " + ex.Message;
            }

            return RedirectToAction(nameof(ServiceLinks));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteServiceLink(int id)
        {
            try
            {
                var link = await _context.ServiceLinks.FindAsync(id);
                if (link != null)
                {
                    _context.ServiceLinks.Remove(link);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "لینک حذف شد";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در حذف: " + ex.Message;
            }
            return RedirectToAction(nameof(ServiceLinks));
        }

        // ✅ Helper Methods
        private int GetCurrentAdminId()
        {
            // روش 1: از ClaimsPrincipal
            var userId = User.FindFirst("AdminId")?.Value;
            if (int.TryParse(userId, out int adminId))
                return adminId;

            // روش 2: اولین AdminUser
            return 1; // یا از Session/Cookie
        }

        private async Task<int> GetNextDisplayOrderAsync()
        {
            var maxOrder = await _context.ServiceLinks.MaxAsync(l => (int?)l.DisplayOrder) ?? 0;
            return maxOrder + 1;
        }


        #endregion







        [HttpGet]
        public async Task<IActionResult> Sms()
        {
            ViewBag.SmsEnabled = await GetSettingValueAsync("SmsEnabled") == "true";
            ViewBag.SmsApiKey = await GetSettingValueAsync("SmsApiKey") ?? "";
            ViewBag.SmsSenderNumber = await GetSettingValueAsync("SmsSenderNumber") ?? "";
            ViewBag.SmsUsername = await GetSettingValueAsync("SmsUsername") ?? "";        // ✅ TopTip Username
            ViewBag.SmsPassword = await GetSettingValueAsync("SmsPassword") ?? "";        // ✅ TopTip Password
            ViewBag.SmsApiUrl = await GetSettingValueAsync("SmsApiUrl") ?? "";
            ViewBag.SmsProvider = await GetSettingValueAsync("SmsProvider") ?? "TopTip";
            ViewBag.OtpLength = await GetSettingValueAsync("OtpLength") ?? "6";
            ViewBag.OtpExpiryMinutes = await GetSettingValueAsync("OtpExpiryMinutes") ?? "5";

            // ✅ تست Balance
            ViewBag.BalanceResult = await CheckTopTipBalanceAsync();

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms(bool smsEnabled, string smsApiKey, string smsSenderNumber,
            string smsUsername, string smsPassword, string smsApiUrl, string smsProvider,
            int otpLength = 6, int otpExpiryMinutes = 5)
        {
            try
            {
                await SaveSettingAsync("SmsEnabled", smsEnabled.ToString().ToLower());
                await SaveSettingAsync("SmsApiKey", smsApiKey);
                await SaveSettingAsync("SmsSenderNumber", smsSenderNumber);
                await SaveSettingAsync("SmsUsername", smsUsername);        // ✅
                await SaveSettingAsync("SmsPassword", smsPassword);        // ✅
                await SaveSettingAsync("SmsApiUrl", smsApiUrl);
                await SaveSettingAsync("SmsProvider", smsProvider);
                await SaveSettingAsync("OtpLength", otpLength.ToString());
                await SaveSettingAsync("OtpExpiryMinutes", otpExpiryMinutes.ToString());

                TempData["Success"] = "تنظیمات TopTip کامل ذخیره شد";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا: " + ex.Message;
            }
            return RedirectToAction(nameof(Sms));
        }

        // ✅ Balance Checker
        // ✅ Balance Checker - اصلاح کامل
        private async Task<object> CheckTopTipBalanceAsync()
        {
            try
            {
                var username = await GetSettingValueAsync("SmsUsername");
                var password = await GetSettingValueAsync("SmsPassword");

                // 🔥 DEBUG - لاگ برای تست
                System.Diagnostics.Debug.WriteLine($"🔥 TopTip Balance: Username={username?.Substring(0, 3)}..., Password={password?.Length} chars");

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return new { Success = false, Balance = 0M, Message = "نام کاربری/رمز خالی است" };

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                string apiUrl = $"http://toptip.ir/webservice/rest/user_info" +
                               $"?login_username={Uri.EscapeDataString(username)}" +
                               $"&login_password={Uri.EscapeDataString(password)}";

                var response = await httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                // 🔥 DEBUG Response
                System.Diagnostics.Debug.WriteLine($"🔥 TopTip Response: {content.Substring(0, Math.Min(200, content.Length))}...");

                if (response.IsSuccessStatusCode)
                {
                    // ✅ چک result:false (خطا)
                    if (content.Contains("\"result\":false") || content.Contains("\"result\": false"))
                    {
                        return new { Success = false, Balance = 0M, Message = "نام کاربری/رمز اشتباه" };
                    }

                    var balance = ParseTopTipBalance(content);
                    return new
                    {
                        Success = true,
                        Balance = balance,
                        Message = balance > 0 ? "موفق" : "پاسخ نامعتبر"
                    };
                }
                else
                {
                    return new { Success = false, Balance = 0M, Message = $"HTTP {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔥 Balance Error: {ex.Message}");
                return new { Success = false, Balance = 0M, Message = "خطای اتصال" };
            }
        }

        // ✅ Parse اصلاح‌شده
        private decimal ParseTopTipBalance(string response)
        {
            try
            {
                // 🔥 DEBUG Raw Response
                System.Diagnostics.Debug.WriteLine($"🔥 Parsing: {response.Substring(0, 300)}");

                // ✅ دقیق‌تر: "cash":"90582307"
                var cashIndex = response.IndexOf("\"cash\":");
                if (cashIndex == -1)
                {
                    System.Diagnostics.Debug.WriteLine("🔥 'cash' not found");
                    return 0;
                }

                var startIndex = response.IndexOf("\"", cashIndex + 7) + 1; // بعد از "cash":
                var endIndex = response.IndexOf("\"", startIndex);

                if (startIndex <= 0 || endIndex == -1 || endIndex <= startIndex)
                {
                    System.Diagnostics.Debug.WriteLine("🔥 Invalid cash format");
                    return 0;
                }

                var cashStr = response.Substring(startIndex, endIndex - startIndex).Trim();
                System.Diagnostics.Debug.WriteLine($"🔥 Extracted cash: '{cashStr}'");

                return decimal.TryParse(cashStr, out decimal balance) ? balance : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔥 Parse Error: {ex.Message}");
                return 0;
            }
        }
















    }
}
