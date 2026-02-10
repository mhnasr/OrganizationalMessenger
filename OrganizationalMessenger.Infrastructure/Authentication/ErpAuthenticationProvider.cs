using Microsoft.Extensions.Configuration;
using OrganizationalMessenger.Application.Interfaces;
using System.Net.Http.Json;

namespace OrganizationalMessenger.Infrastructure.Authentication
{
    public class ErpAuthenticationProvider : IAuthenticationProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ErpAuthenticationProvider(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                var apiUrl = _configuration["Authentication:ERP:ApiUrl"];
                var apiKey = _configuration["Authentication:ERP:ApiKey"];

                var request = new
                {
                    username = username,
                    password = password
                };

                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{apiUrl}/api/auth/validate",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "نام کاربری یا رمز عبور اشتباه است."
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<ErpAuthResponse>();

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    UserId = result?.UserId,
                    FirstName = result?.FirstName,
                    LastName = result?.LastName,
                    Email = result?.Email,
                    PhoneNumber = result?.PhoneNumber
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"خطا در اتصال به سیستم ERP: {ex.Message}"
                };
            }
        }

        private class ErpAuthResponse
        {
            public string? UserId { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
        }
    }
}
