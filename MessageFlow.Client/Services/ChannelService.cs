using System.Net.Http.Json;
using MessageFlow.Client.Models;
using MessageFlow.Client.Models.DTOs;

namespace MessageFlow.Client.Services
{
    public class ChannelService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChannelService> _logger;

        public ChannelService(HttpClient httpClient, ILogger<ChannelService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<FacebookSettingsDTO?> GetFacebookSettingsAsync(string companyId)
        {
            return await _httpClient.GetFromJsonAsync<FacebookSettingsDTO>($"api/channels/facebook/{companyId}");
        }

        public async Task<bool> SaveFacebookSettingsAsync(string companyId, FacebookSettingsDTO settings)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/channels/facebook/{companyId}", settings);
            return response.IsSuccessStatusCode;
        }

        public async Task<WhatsAppSettingsDTO?> GetWhatsAppSettingsAsync(string companyId)
        {
            return await _httpClient.GetFromJsonAsync<WhatsAppSettingsDTO>($"api/channels/whatsapp/{companyId}");
        }

        public async Task<NotificationResult> SaveWhatsCoreAppSettingsAsync(WhatsAppCoreSettingsDTO coreSettings)
        {
            var response = await _httpClient.PostAsJsonAsync("api/channels/whatsapp/settings", coreSettings);
            var message = await response.Content.ReadAsStringAsync();
            return new NotificationResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = message
            };
        }

        public async Task<NotificationResult> SavePhoneNumbersAsync(List<PhoneNumberInfoDTO> numbers)
        {
            var response = await _httpClient.PostAsJsonAsync("api/channels/whatsapp/numbers", numbers);


            var message = await response.Content.ReadAsStringAsync();

            return new NotificationResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = message
            };
        }



    }
}
