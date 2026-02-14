using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Infrastructure.Authentication;
using OrganizationalMessenger.Infrastructure.Authentication.OrganizationalMessenger.Infrastructure.Authentication;
using OrganizationalMessenger.Infrastructure.Data;
using OrganizationalMessenger.Infrastructure.Services;
using OrganizationalMessenger.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 104857600; // 100 MB
});



// =====================================================
// SERVICES (فقط موارد موجود)
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// ✅ Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ HttpClient
builder.Services.AddHttpClient();
builder.Services.AddScoped<OtpService>();
// ✅ Services موجود
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IAuthenticationManager, AuthenticationManager>();
builder.Services.AddScoped<ISmsSender, TopTipSmsSender>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });
// Providers موجود
builder.Services.AddScoped<IAuthenticationProvider, ActiveDirectoryProvider>();
builder.Services.AddScoped<IAuthenticationProvider, ErpAuthenticationProvider>();
builder.Services.AddScoped<IAuthenticationProvider, OtpAuthenticationProvider>();



builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();






var app = builder.Build();

// =====================================================
// MIDDLEWARE (ترتیب صحیح)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ ترتیب CRITICAL
app.UseSession();
app.UseAuthentication();  // اول
app.UseAuthorization();   // بعد

// =====================================================
// ROUTES
app.MapRazorPages();

// Admin Area
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// ✅ CHAT ROUTE
app.MapControllerRoute(
    name: "chat",
    pattern: "chat/{action=Index}/{id?}",
    defaults: new { controller = "Chat", action = "Index" });

// ✅ DEFAULT ROUTE → مستقیم لاگین!
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");







app.MapHub<ChatHub>("/chatHub");



app.Run();
