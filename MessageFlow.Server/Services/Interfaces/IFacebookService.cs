using MessageFlow.Shared.DTOs;
using System.Text.Json;

namespace MessageFlow.Server.Services.Interfaces
{
    public interface IFacebookService
    {
        Task<FacebookSettingsDTO?> GetFacebookSettingsAsync(string companyId);
        Task<bool> SaveFacebookSettingsAsync(string companyId, FacebookSettingsDTO facebookSettingsDto);
        Task SendMessageToFacebookAsync(string recipientId, string messageText, string companyId, string localMessageId);
        Task ProcessFacebookWebhookEventAsync(JsonElement entry);
    }
}
