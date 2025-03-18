using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;

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

        public async Task<bool> SaveWhatsAppSettingsAsync(string companyId, WhatsAppSettingsDTO settings)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/channels/whatsapp/{companyId}", settings);
            return response.IsSuccessStatusCode;
        }
    }

}
