using MessageFlow.Shared.DTOs;
using System.Text.Json;

namespace MessageFlow.Server.Services.Interfaces
{
    public interface IWhatsAppService
    {
        Task<bool> SaveWhatsAppSettingsAsync(string companyId, WhatsAppSettingsDTO whatsAppSettingsDto);
        Task<WhatsAppSettingsDTO?> GetWhatsAppSettingsAsync(string companyId);
        Task ProcessIncomingMessageAsync(string businessAccountId, JsonElement changes);
        Task SendMessageToWhatsAppAsync(string recipientPhoneNumber, string messageText, string companyId, string localMessageId);
    }
}
