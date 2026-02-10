using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using System.Globalization;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Main Dashboard View

        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var todayStart = today;
            var todayEnd = today.AddDays(1);
            var weekStart = today.AddDays(-7);
            var onlineThreshold = DateTime.Now.AddMinutes(-5);

            // ==================== کارت‌های آمار اصلی ====================

            // کل کاربران
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // کاربران آنلاین
            ViewBag.OnlineUsers = await _context.Users
                .CountAsync(u => u.IsOnline || (u.LastSeen.HasValue && u.LastSeen > onlineThreshold));

            // کاربران جدید امروز
            ViewBag.NewUsersToday = await _context.Users
                .CountAsync(u => u.CreatedAt >= todayStart && u.CreatedAt < todayEnd);

            // کل پیام‌ها
            ViewBag.TotalMessages = await _context.Messages.CountAsync(m => !m.IsDeleted);

            // پیام‌های امروز
            ViewBag.MessagesToday = await _context.Messages
                .CountAsync(m => m.CreatedAt >= todayStart && m.CreatedAt < todayEnd && !m.IsDeleted);

            // کل گروه‌ها
            ViewBag.TotalGroups = await _context.Groups.CountAsync(g => g.IsActive);

            // گروه‌های جدید این هفته
            ViewBag.NewGroupsThisWeek = await _context.Groups
                .CountAsync(g => g.CreatedAt >= weekStart && g.IsActive);

            // کل کانال‌ها
            ViewBag.TotalChannels = await _context.Channels.CountAsync(c => c.IsActive);

            // کانال‌های جدید این هفته
            ViewBag.NewChannelsThisWeek = await _context.Channels
                .CountAsync(c => c.CreatedAt >= weekStart && c.IsActive);

            // کل تماس‌ها
            ViewBag.TotalCalls = await _context.Calls.CountAsync();

            // تماس‌های امروز
            ViewBag.CallsToday = await _context.Calls
                .CountAsync(c => c.StartedAt >= todayStart && c.StartedAt < todayEnd);

            // کل فایل‌ها (استفاده از AttachmentUrl به جای FileUrl)
            ViewBag.TotalFiles = await _context.Messages
                .CountAsync(m => !m.IsDeleted && !string.IsNullOrEmpty(m.AttachmentUrl));

            // حجم کل فایل‌ها (استفاده از AttachmentSize به جای FileSize)
            var totalStorageBytes = await _context.Messages
                .Where(m => !m.IsDeleted && m.AttachmentSize.HasValue)
                .SumAsync(m => m.AttachmentSize ?? 0);
            ViewBag.TotalStorageUsed = FormatFileSize(totalStorageBytes);

            // گزارشات در انتظار
            ViewBag.PendingReports = await GetPendingReportsCountAsync();

            // ==================== داده‌های نمودار پیام‌ها (هفتگی) ====================
            var messageChartData = await GetMessageChartDataAsync(7);
            ViewBag.MessageChartLabels = System.Text.Json.JsonSerializer.Serialize(messageChartData.Labels);
            ViewBag.MessageChartData = System.Text.Json.JsonSerializer.Serialize(messageChartData.Values);

            // ==================== داده‌های نمودار نوع پیام‌ها ====================
            var messageTypesData = await GetMessageTypesDataAsync(30);
            ViewBag.MessageTypesData = System.Text.Json.JsonSerializer.Serialize(messageTypesData);

            // ==================== داده‌های نمودار فعالیت کاربران (ساعتی) ====================
            var userActivityData = await GetUserActivityDataAsync();
            ViewBag.UserActivityData = System.Text.Json.JsonSerializer.Serialize(userActivityData);

            // ==================== داده‌های نمودار تماس‌ها ====================
            var callsChartData = await GetCallsChartDataAsync(7);
            ViewBag.CallsChartLabels = System.Text.Json.JsonSerializer.Serialize(callsChartData.Labels);
            ViewBag.VoiceCallsData = System.Text.Json.JsonSerializer.Serialize(callsChartData.VoiceCalls);
            ViewBag.VideoCallsData = System.Text.Json.JsonSerializer.Serialize(callsChartData.VideoCalls);

            // ==================== جدول کاربران جدید ====================
            ViewBag.NewUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new NewUserViewModel
                {
                    Id = u.Id,
                    FullName = !string.IsNullOrEmpty(u.FullName)
                        ? u.FullName
                        : $"{u.FirstName} {u.LastName}".Trim(),
                    Username = u.Username,
                    Email = u.Email ?? "",
                    CreatedAt = u.CreatedAt,
                    CreatedAtPersian = ToPersianDate(u.CreatedAt),
                    IsActive = u.IsActive,
                    IsOnline = u.IsOnline
                })
                .ToListAsync();

            // ==================== جدول گزارشات اخیر ====================
            ViewBag.RecentReports = await GetRecentReportsAsync(5);

            // ==================== جدول گروه‌های فعال ====================
            ViewBag.ActiveGroups = await _context.Groups
                .Where(g => g.IsActive)
                .OrderByDescending(g => g.Messages.Count)
                .Take(5)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    MemberCount = g.UserGroups.Count,
                    MessageCount = g.Messages.Count(m => !m.IsDeleted)
                })
                .ToListAsync();

            // ==================== جدول کانال‌های فعال ====================
            ViewBag.ActiveChannels = await _context.Channels
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.UserChannels.Count)
                .Take(5)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    SubscriberCount = c.UserChannels.Count,
                    PostCount = c.Messages.Count(m => !m.IsDeleted)
                })
                .ToListAsync();

            // ==================== جدول آخرین تماس‌ها ====================
            ViewBag.RecentCalls = await _context.Calls
                .Include(c => c.Initiator)
                .OrderByDescending(c => c.StartedAt)
                .Take(5)
                .Select(c => new
                {
                    c.Id,
                    InitiatorName = !string.IsNullOrEmpty(c.Initiator.FullName)
                        ? c.Initiator.FullName
                        : $"{c.Initiator.FirstName} {c.Initiator.LastName}".Trim(),
                    Type = c.Type == CallType.Voice ? "صوتی" : "تصویری",
                    Duration = FormatDuration(c.Duration),
                    Status = GetCallStatusPersian(c.Status)
                })
                .ToListAsync();

            // ==================== تاریخ شمسی ====================
            ViewBag.TodayPersianDate = GetPersianDateFull();
            ViewBag.TodayPersianDateTime = GetPersianDateTime();

            return View();
        }

        #endregion

        #region Chart Data API Endpoints

        /// <summary>
        /// دریافت داده‌های نمودار بر اساس نوع و بازه زمانی
        /// </summary>
        [HttpGet("GetChartData")]
        public async Task<IActionResult> GetChartData([FromQuery] string type, [FromQuery] string period)
        {
            int days = period switch
            {
                "week" => 7,
                "month" => 30,
                "year" => 365,
                _ => 7
            };

            if (type == "messages")
            {
                var data = await GetMessageChartDataAsync(days);
                return Json(new
                {
                    labels = data.Labels,
                    values = data.Values
                });
            }
            else if (type == "calls")
            {
                var data = await GetCallsChartDataAsync(days);
                return Json(new
                {
                    labels = data.Labels,
                    voiceCalls = data.VoiceCalls,
                    videoCalls = data.VideoCalls
                });
            }

            return Json(new { error = "Invalid type" });
        }

        /// <summary>
        /// دریافت آمار پیام‌ها برای نمودار
        /// </summary>
        [HttpGet("api/messages-stats")]
        public async Task<IActionResult> GetMessagesStats([FromQuery] int days = 7)
        {
            var data = await GetMessageChartDataAsync(days);
            return Json(new
            {
                success = true,
                labels = data.Labels,
                values = data.Values,
                total = data.Values.Sum()
            });
        }

        /// <summary>
        /// دریافت آمار نوع پیام‌ها برای نمودار دایره‌ای
        /// </summary>
        [HttpGet("api/message-types")]
        public async Task<IActionResult> GetMessageTypes([FromQuery] int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days);

            var messageTypes = await _context.Messages
                .Where(m => m.CreatedAt >= startDate && !m.IsDeleted)
                .GroupBy(m => m.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var result = messageTypes.Select(mt => new
            {
                Type = mt.Type.ToString(),
                TypePersian = GetMessageTypePersianName(mt.Type),
                Count = mt.Count,
                Color = GetMessageTypeColor(mt.Type)
            }).OrderByDescending(x => x.Count).ToList();

            return Json(new
            {
                success = true,
                data = result,
                labels = result.Select(r => r.TypePersian).ToArray(),
                values = result.Select(r => r.Count).ToArray(),
                colors = result.Select(r => r.Color).ToArray()
            });
        }

        /// <summary>
        /// دریافت آمار کاربران جدید
        /// </summary>
        [HttpGet("api/users-stats")]
        public async Task<IActionResult> GetUsersStats([FromQuery] int days = 7)
        {
            var endDate = DateTime.Today.AddDays(1);
            var startDate = DateTime.Today.AddDays(-days + 1);

            var usersPerDay = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt < endDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<int>();

            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                labels.Add(GetPersianDayName(date));
                var dayData = usersPerDay.FirstOrDefault(x => x.Date == date);
                values.Add(dayData?.Count ?? 0);
            }

            return Json(new
            {
                success = true,
                labels = labels,
                values = values,
                total = values.Sum()
            });
        }

        /// <summary>
        /// دریافت آمار تماس‌ها
        /// </summary>
        [HttpGet("api/calls-stats")]
        public async Task<IActionResult> GetCallsStats([FromQuery] int days = 7)
        {
            var data = await GetCallsChartDataAsync(days);
            return Json(new
            {
                success = true,
                labels = data.Labels,
                voiceCalls = data.VoiceCalls,
                videoCalls = data.VideoCalls,
                totalVoice = data.VoiceCalls.Sum(),
                totalVideo = data.VideoCalls.Sum()
            });
        }

        /// <summary>
        /// دریافت فعالیت ساعتی کاربران (24 ساعت گذشته)
        /// </summary>
        [HttpGet("api/hourly-activity")]
        public async Task<IActionResult> GetHourlyActivity()
        {
            var data = await GetUserActivityDataAsync();
            return Json(new
            {
                success = true,
                labels = data.Labels,
                values = data.Values,
                peakHour = data.Labels[Array.IndexOf(data.Values.ToArray(), data.Values.Max())]
            });
        }

        #endregion

        #region Table Data API Endpoints

        /// <summary>
        /// دریافت لیست کاربران جدید
        /// </summary>
        [HttpGet("api/new-users")]
        public async Task<IActionResult> GetNewUsers([FromQuery] int count = 10)
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .Select(u => new
                {
                    u.Id,
                    FullName = !string.IsNullOrEmpty(u.FullName)
                        ? u.FullName
                        : $"{u.FirstName} {u.LastName}".Trim(),
                    u.Username,
                    Email = u.Email ?? "",
                    u.CreatedAt,
                    CreatedAtPersian = ToPersianDate(u.CreatedAt),
                    u.IsActive,
                    u.IsOnline
                })
                .ToListAsync();

            return Json(new { success = true, data = users });
        }

        /// <summary>
        /// دریافت لیست گزارشات اخیر
        /// </summary>
        [HttpGet("api/recent-reports")]
        public async Task<IActionResult> GetRecentReportsApi([FromQuery] int count = 10)
        {
            var reports = await GetRecentReportsAsync(count);
            return Json(new { success = true, data = reports });
        }

        /// <summary>
        /// دریافت لیست گروه‌های فعال
        /// </summary>
        [HttpGet("api/active-groups")]
        public async Task<IActionResult> GetActiveGroups([FromQuery] int count = 10)
        {
            var groups = await _context.Groups
                .Where(g => g.IsActive)
                .OrderByDescending(g => g.Messages.Count)
                .Take(count)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    MemberCount = g.UserGroups.Count,
                    MessageCount = g.Messages.Count(m => !m.IsDeleted),
                    CreatedAt = g.CreatedAt,
                    CreatedAtPersian = ToPersianDate(g.CreatedAt)
                })
                .ToListAsync();

            return Json(new { success = true, data = groups });
        }

        /// <summary>
        /// دریافت لیست کانال‌های فعال
        /// </summary>
        [HttpGet("api/active-channels")]
        public async Task<IActionResult> GetActiveChannels([FromQuery] int count = 10)
        {
            var channels = await _context.Channels
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.UserChannels.Count)
                .Take(count)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    SubscriberCount = c.UserChannels.Count,
                    PostCount = c.Messages.Count(m => !m.IsDeleted),
                    CreatedAt = c.CreatedAt,
                    CreatedAtPersian = ToPersianDate(c.CreatedAt)
                })
                .ToListAsync();

            return Json(new { success = true, data = channels });
        }

        /// <summary>
        /// دریافت آخرین تماس‌ها
        /// </summary>
        [HttpGet("api/recent-calls")]
        public async Task<IActionResult> GetRecentCalls([FromQuery] int count = 10)
        {
            var calls = await _context.Calls
                .Include(c => c.Initiator)
                .OrderByDescending(c => c.StartedAt)
                .Take(count)
                .Select(c => new
                {
                    c.Id,
                    InitiatorId = c.InitiatorId,
                    InitiatorName = !string.IsNullOrEmpty(c.Initiator.FullName)
                        ? c.Initiator.FullName
                        : $"{c.Initiator.FirstName} {c.Initiator.LastName}".Trim(),
                    Type = c.Type.ToString(),
                    TypePersian = c.Type == CallType.Voice ? "صوتی" : "تصویری",
                    c.Duration,
                    DurationFormatted = FormatDuration(c.Duration),
                    Status = c.Status.ToString(),
                    StatusPersian = GetCallStatusPersian(c.Status),
                    c.StartedAt,
                    StartedAtPersian = ToPersianDateTime(c.StartedAt)
                })
                .ToListAsync();

            return Json(new { success = true, data = calls });
        }

        #endregion

        #region Real-time Stats API

        /// <summary>
        /// دریافت آمار لحظه‌ای برای به‌روزرسانی داشبورد
        /// </summary>
        [HttpGet("api/realtime-stats")]
        public async Task<IActionResult> GetRealtimeStats()
        {
            var today = DateTime.Today;
            var todayEnd = today.AddDays(1);
            var onlineThreshold = DateTime.Now.AddMinutes(-5);

            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                OnlineUsers = await _context.Users
                    .CountAsync(u => u.IsOnline || (u.LastSeen.HasValue && u.LastSeen > onlineThreshold)),
                TotalMessages = await _context.Messages.CountAsync(m => !m.IsDeleted),
                MessagesToday = await _context.Messages
                    .CountAsync(m => m.CreatedAt >= today && m.CreatedAt < todayEnd && !m.IsDeleted),
                TotalGroups = await _context.Groups.CountAsync(g => g.IsActive),
                TotalChannels = await _context.Channels.CountAsync(c => c.IsActive),
                TotalCalls = await _context.Calls.CountAsync(),
                CallsToday = await _context.Calls
                    .CountAsync(c => c.StartedAt >= today && c.StartedAt < todayEnd),
                PendingReports = await GetPendingReportsCountAsync(),
                LastUpdate = DateTime.Now,
                LastUpdatePersian = GetPersianDateTime()
            };

            return Json(new { success = true, data = stats });
        }

        /// <summary>
        /// دریافت آمار کامل داشبورد
        /// </summary>
        [HttpGet("api/full-stats")]
        public async Task<IActionResult> GetFullStats()
        {
            var today = DateTime.Today;
            var todayEnd = today.AddDays(1);
            var weekStart = today.AddDays(-7);
            var onlineThreshold = DateTime.Now.AddMinutes(-5);

            // آمار اصلی
            var totalUsers = await _context.Users.CountAsync();
            var onlineUsers = await _context.Users
                .CountAsync(u => u.IsOnline || (u.LastSeen.HasValue && u.LastSeen > onlineThreshold));
            var newUsersToday = await _context.Users
                .CountAsync(u => u.CreatedAt >= today && u.CreatedAt < todayEnd);

            var totalMessages = await _context.Messages.CountAsync(m => !m.IsDeleted);
            var messagesToday = await _context.Messages
                .CountAsync(m => m.CreatedAt >= today && m.CreatedAt < todayEnd && !m.IsDeleted);

            var totalGroups = await _context.Groups.CountAsync(g => g.IsActive);
            var newGroupsThisWeek = await _context.Groups
                .CountAsync(g => g.CreatedAt >= weekStart && g.IsActive);

            var totalChannels = await _context.Channels.CountAsync(c => c.IsActive);
            var newChannelsThisWeek = await _context.Channels
                .CountAsync(c => c.CreatedAt >= weekStart && c.IsActive);

            var totalCalls = await _context.Calls.CountAsync();
            var callsToday = await _context.Calls
                .CountAsync(c => c.StartedAt >= today && c.StartedAt < todayEnd);

            var totalFiles = await _context.Messages
                .CountAsync(m => !m.IsDeleted && !string.IsNullOrEmpty(m.AttachmentUrl));

            var totalStorageBytes = await _context.Messages
                .Where(m => !m.IsDeleted && m.AttachmentSize.HasValue)
                .SumAsync(m => m.AttachmentSize ?? 0);

            var pendingReports = await GetPendingReportsCountAsync();

            // نمودارها
            var messageChartData = await GetMessageChartDataAsync(7);
            var messageTypesData = await GetMessageTypesDataAsync(30);
            var userActivityData = await GetUserActivityDataAsync();
            var callsChartData = await GetCallsChartDataAsync(7);

            return Json(new
            {
                success = true,
                data = new
                {
                    // آمار کارت‌ها
                    Cards = new
                    {
                        TotalUsers = totalUsers,
                        OnlineUsers = onlineUsers,
                        NewUsersToday = newUsersToday,
                        TotalMessages = totalMessages,
                        MessagesToday = messagesToday,
                        TotalGroups = totalGroups,
                        NewGroupsThisWeek = newGroupsThisWeek,
                        TotalChannels = totalChannels,
                        NewChannelsThisWeek = newChannelsThisWeek,
                        TotalCalls = totalCalls,
                        CallsToday = callsToday,
                        TotalFiles = totalFiles,
                        TotalStorageUsed = FormatFileSize(totalStorageBytes),
                        PendingReports = pendingReports
                    },
                    // نمودارها
                    Charts = new
                    {
                        Messages = new
                        {
                            Labels = messageChartData.Labels,
                            Values = messageChartData.Values
                        },
                        MessageTypes = messageTypesData,
                        UserActivity = new
                        {
                            Labels = userActivityData.Labels,
                            Values = userActivityData.Values
                        },
                        Calls = new
                        {
                            Labels = callsChartData.Labels,
                            VoiceCalls = callsChartData.VoiceCalls,
                            VideoCalls = callsChartData.VideoCalls
                        }
                    },
                    // متادیتا
                    Meta = new
                    {
                        LastUpdate = DateTime.Now,
                        LastUpdatePersian = GetPersianDateTime(),
                        TodayPersian = GetPersianDateFull()
                    }
                }
            });
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// دریافت داده‌های نمودار پیام‌ها
        /// </summary>
        private async Task<ChartData> GetMessageChartDataAsync(int days)
        {
            var endDate = DateTime.Today.AddDays(1);
            var startDate = DateTime.Today.AddDays(-days + 1);

            var messagesPerDay = await _context.Messages
                .Where(m => m.CreatedAt >= startDate && m.CreatedAt < endDate && !m.IsDeleted)
                .GroupBy(m => m.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<int>();

            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                labels.Add(GetPersianDayName(date));
                var dayData = messagesPerDay.FirstOrDefault(x => x.Date == date);
                values.Add(dayData?.Count ?? 0);
            }

            return new ChartData { Labels = labels, Values = values };
        }

        /// <summary>
        /// دریافت داده‌های نوع پیام‌ها
        /// </summary>
        private async Task<List<MessageTypeData>> GetMessageTypesDataAsync(int days)
        {
            var startDate = DateTime.Today.AddDays(-days);

            var messageTypes = await _context.Messages
                .Where(m => m.CreatedAt >= startDate && !m.IsDeleted)
                .GroupBy(m => m.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return messageTypes.Select(mt => new MessageTypeData
            {
                Type = mt.Type.ToString(),
                TypePersian = GetMessageTypePersianName(mt.Type),
                Count = mt.Count,
                Color = GetMessageTypeColor(mt.Type)
            }).OrderByDescending(x => x.Count).ToList();
        }

        /// <summary>
        /// دریافت داده‌های فعالیت ساعتی کاربران
        /// </summary>
        private async Task<HourlyActivityData> GetUserActivityDataAsync()
        {
            var last24Hours = DateTime.Now.AddHours(-24);

            var hourlyActivity = await _context.Messages
                .Where(m => m.CreatedAt >= last24Hours && !m.IsDeleted)
                .GroupBy(m => m.CreatedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<int>();

            for (int hour = 0; hour < 24; hour++)
            {
                labels.Add($"{hour:00}:00");
                var hourData = hourlyActivity.FirstOrDefault(x => x.Hour == hour);
                values.Add(hourData?.Count ?? 0);
            }

            return new HourlyActivityData { Labels = labels, Values = values };
        }

        /// <summary>
        /// دریافت داده‌های نمودار تماس‌ها
        /// </summary>
        private async Task<CallsChartData> GetCallsChartDataAsync(int days)
        {
            var endDate = DateTime.Today.AddDays(1);
            var startDate = DateTime.Today.AddDays(-days + 1);

            var callsPerDay = await _context.Calls
                .Where(c => c.StartedAt >= startDate && c.StartedAt < endDate)
                .GroupBy(c => new { Date = c.StartedAt.Date, c.Type })
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.Type,
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = new List<string>();
            var voiceCalls = new List<int>();
            var videoCalls = new List<int>();

            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                labels.Add(GetPersianDayName(date));

                var voiceCount = callsPerDay
                    .FirstOrDefault(x => x.Date == date && x.Type == CallType.Voice)?.Count ?? 0;
                var videoCount = callsPerDay
                    .FirstOrDefault(x => x.Date == date && x.Type == CallType.Video)?.Count ?? 0;

                voiceCalls.Add(voiceCount);
                videoCalls.Add(videoCount);
            }

            return new CallsChartData
            {
                Labels = labels,
                VoiceCalls = voiceCalls,
                VideoCalls = videoCalls
            };
        }

        /// <summary>
        /// دریافت تعداد گزارشات در انتظار
        /// </summary>
        private async Task<int> GetPendingReportsCountAsync()
        {
            // اگر Entity Report وجود دارد
            if (_context.Model.FindEntityType(typeof(Report)) != null)
            {
                return await _context.Set<Report>()
                    .CountAsync(r => r.Status == ReportStatus.Pending);
            }
            return 0;
        }

        /// <summary>
        /// دریافت گزارشات اخیر
        /// </summary>
        private async Task<List<ReportViewModel>> GetRecentReportsAsync(int count)
        {
            // اگر Entity Report وجود دارد
            if (_context.Model.FindEntityType(typeof(Report)) != null)
            {
                return await _context.Set<Report>()
                    .Include(r => r.Reporter)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(count)
                    .Select(r => new ReportViewModel
                    {
                        Id = r.Id,
                        ReporterName = !string.IsNullOrEmpty(r.Reporter.FullName)
                            ? r.Reporter.FullName
                            : $"{r.Reporter.FirstName} {r.Reporter.LastName}".Trim(),
                        ItemType = r.ItemType.ToString(),
                        ItemTypePersian = GetReportItemTypePersian(r.ItemType),
                        Reason = r.Reason ?? "",
                        Status = r.Status.ToString(),
                        StatusPersian = GetReportStatusPersian(r.Status),
                        StatusClass = GetReportStatusClass(r.Status),
                        CreatedAt = r.CreatedAt,
                        CreatedAtPersian = ToPersianDate(r.CreatedAt)
                    })
                    .ToListAsync();
            }
            return new List<ReportViewModel>();
        }

        #endregion

        #region Persian Date/Time Helpers

        /// <summary>
        /// تبدیل تاریخ به شمسی
        /// </summary>
        private static string ToPersianDate(DateTime date)
        {
            var pc = new PersianCalendar();
            return $"{pc.GetYear(date)}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
        }

        /// <summary>
        /// تبدیل تاریخ و زمان به شمسی
        /// </summary>
        private static string ToPersianDateTime(DateTime date)
        {
            var pc = new PersianCalendar();
            return $"{pc.GetYear(date)}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00} - {date:HH:mm}";
        }

        /// <summary>
        /// دریافت نام روز به فارسی
        /// </summary>
        private static string GetPersianDayName(DateTime date)
        {
            var pc = new PersianCalendar();
            var dayOfWeek = pc.GetDayOfWeek(date);

            var persianDayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Saturday, "شنبه" },
                { DayOfWeek.Sunday, "یکشنبه" },
                { DayOfWeek.Monday, "دوشنبه" },
                { DayOfWeek.Tuesday, "سه‌شنبه" },
                { DayOfWeek.Wednesday, "چهارشنبه" },
                { DayOfWeek.Thursday, "پنجشنبه" },
                { DayOfWeek.Friday, "جمعه" }
            };

            return persianDayNames[dayOfWeek];
        }

        /// <summary>
        /// دریافت تاریخ شمسی کامل
        /// </summary>
        private static string GetPersianDateFull()
        {
            var pc = new PersianCalendar();
            var now = DateTime.Now;

            var persianMonthNames = new[]
            {
                "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
                "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
            };

            var dayOfWeek = GetPersianDayName(now);
            var day = pc.GetDayOfMonth(now);
            var month = persianMonthNames[pc.GetMonth(now)];
            var year = pc.GetYear(now);

            return $"{dayOfWeek}، {day} {month} {year}";
        }

        /// <summary>
        /// دریافت تاریخ و زمان شمسی
        /// </summary>
        private static string GetPersianDateTime()
        {
            var pc = new PersianCalendar();
            var now = DateTime.Now;
            return $"{pc.GetYear(now)}/{pc.GetMonth(now):00}/{pc.GetDayOfMonth(now):00} - {now:HH:mm:ss}";
        }

        #endregion

        #region Enum Translation Helpers

        /// <summary>
        /// دریافت نام فارسی نوع پیام - اصلاح شده بر اساس Enum واقعی
        /// </summary>
        private static string GetMessageTypePersianName(MessageType type)
        {
            return type switch
            {
                MessageType.Text => "متن",
                MessageType.Image => "تصویر",
                MessageType.Video => "ویدیو",
                MessageType.Audio => "صوت",
                MessageType.Voice => "پیام صوتی",
                MessageType.File => "فایل",
                MessageType.Location => "موقعیت",
                MessageType.Contact => "مخاطب",
                MessageType.Poll => "نظرسنجی",
                MessageType.System => "سیستمی",
                _ => "نامشخص"
            };
        }


        /// <summary>
        /// دریافت رنگ نوع پیام برای نمودار - اصلاح شده بر اساس Enum واقعی
        /// </summary>
        private static string GetMessageTypeColor(MessageType type)
        {
            return type switch
            {
                MessageType.Text => "#3B82F6",      // آبی
                MessageType.Image => "#10B981",     // سبز
                MessageType.Video => "#F59E0B",     // نارنجی
                MessageType.Audio => "#8B5CF6",     // بنفش
                MessageType.Voice => "#EC4899",     // صورتی
                MessageType.File => "#EF4444",      // قرمز
                MessageType.Location => "#06B6D4",  // فیروزه‌ای
                MessageType.Contact => "#6366F1",   // نیلی
                MessageType.Poll => "#14B8A6",      // تیل
                MessageType.System => "#78716C",    // خاکستری تیره
                _ => "#9CA3AF"                      // خاکستری روشن
            };
        }


        /// <summary>
        /// دریافت وضعیت فارسی تماس - اصلاح شده بر اساس Enum واقعی
        /// </summary>
        private static string GetCallStatusPersian(CallStatus status)
        {
            return status switch
            {
                CallStatus.Initiated => "شروع شده",
                CallStatus.Ringing => "در حال زنگ زدن",
                CallStatus.InProgress => "در جریان",
                CallStatus.Ended => "پایان یافته",
                CallStatus.Missed => "بی‌پاسخ",
                CallStatus.Rejected => "رد شده",      // ✅ Rejected وجود دارد
                CallStatus.Failed => "ناموفق",
                _ => "نامشخص"
            };
        }


        /// <summary>
        /// دریافت نام فارسی نوع آیتم گزارش
        /// </summary>
        private static string GetReportItemTypePersian(ReportItemType type)
        {
            return type switch
            {
                ReportItemType.User => "کاربر",
                ReportItemType.Message => "پیام",
                ReportItemType.Group => "گروه",
                ReportItemType.Channel => "کانال",
                _ => "نامشخص"
            };
        }

        /// <summary>
        /// دریافت وضعیت فارسی گزارش - اصلاح شده بر اساس Enum واقعی
        /// </summary>
        private static string GetReportStatusPersian(ReportStatus status)
        {
            return status switch
            {
                ReportStatus.Pending => "در انتظار بررسی",    // 0
                ReportStatus.Reviewed => "بررسی شده",         // 1 (نه UnderReview)
                ReportStatus.Resolved => "حل شده",            // 2
                ReportStatus.Dismissed => "رد شده",           // 3 (نه Rejected)
                _ => "نامشخص"
            };
        }


        /// <summary>
        /// دریافت کلاس CSS برای وضعیت گزارش - اصلاح شده بر اساس Enum واقعی
        /// </summary>
        private static string GetReportStatusClass(ReportStatus status)
        {
            return status switch
            {
                ReportStatus.Pending => "bg-warning text-dark",
                ReportStatus.Reviewed => "bg-info text-white",      // ✅ Reviewed
                ReportStatus.Resolved => "bg-success text-white",
                ReportStatus.Dismissed => "bg-secondary text-white", // ✅ Dismissed
                _ => "bg-light text-dark"
            };
        }


        #endregion

        #region Formatting Helpers

        /// <summary>
        /// فرمت کردن حجم فایل
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "بایت", "کیلوبایت", "مگابایت", "گیگابایت", "ترابایت" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// فرمت کردن مدت زمان تماس
        /// </summary>
        private static string FormatDuration(int? seconds)
        {
            if (!seconds.HasValue || seconds.Value == 0)
                return "0:00";

            var ts = TimeSpan.FromSeconds(seconds.Value);

            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}";

            return $"{ts.Minutes}:{ts.Seconds:00}";
        }

        #endregion
    }

    #region DTOs and ViewModels

    /// <summary>
    /// داده‌های نمودار عمومی
    /// </summary>
    public class ChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
    }

    /// <summary>
    /// داده‌های نوع پیام
    /// </summary>
    public class MessageTypeData
    {
        public string Type { get; set; } = "";
        public string TypePersian { get; set; } = "";
        public int Count { get; set; }
        public string Color { get; set; } = "";
    }

    /// <summary>
    /// داده‌های فعالیت ساعتی
    /// </summary>
    public class HourlyActivityData
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
    }

    /// <summary>
    /// داده‌های نمودار تماس‌ها
    /// </summary>
    public class CallsChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<int> VoiceCalls { get; set; } = new();
        public List<int> VideoCalls { get; set; } = new();
    }

    /// <summary>
    /// ویومدل کاربر جدید
    /// </summary>
    public class NewUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedAtPersian { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// ویومدل گزارش
    /// </summary>
    public class ReportViewModel
    {
        public int Id { get; set; }
        public string ReporterName { get; set; } = "";
        public string ItemType { get; set; } = "";
        public string ItemTypePersian { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusPersian { get; set; } = "";
        public string StatusClass { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedAtPersian { get; set; } = "";
    }

    #endregion
}
