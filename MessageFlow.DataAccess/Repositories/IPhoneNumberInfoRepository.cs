using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IPhoneNumberInfoRepository
    {
        Task<PhoneNumberInfo?> GetPhoneNumberByIdAsync(int id);
        Task<List<PhoneNumberInfo>> GetAllAsync();
        Task<List<PhoneNumberInfo>> GetPhoneNumbersByWhatsAppSettingsAsync(string settingsId);
        Task AddEntityAsync(PhoneNumberInfo phoneNumber);
        Task UpdateEntityAsync(PhoneNumberInfo phoneNumber);
        Task RemoveEntityAsync(PhoneNumberInfo phoneNumber);
    }
}
