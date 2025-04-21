using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IWhatsAppSettingsRepository
    {
        Task<WhatsAppSettingsModel?> GetSettingsByCompanyIdAsync(string companyId);
        Task AddEntityAsync(WhatsAppSettingsModel settings);
        Task UpdateEntityAsync(WhatsAppSettingsModel settings);
        Task<WhatsAppSettingsModel?> GetSettingsByBusinessAccountIdAsync(string businessAccountId);
    }
}