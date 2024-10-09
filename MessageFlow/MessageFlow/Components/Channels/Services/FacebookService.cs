using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Components.Accounts.Services
{
    public class FacebookService
    {
        private readonly ApplicationDbContext _dbContext;

        public FacebookService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Retrieve Facebook settings for a company
        public async Task<FacebookSettingsModel?> GetFacebookSettingsAsync(int companyId)
        {
            return await _dbContext.FacebookSettingsModels
                .FirstOrDefaultAsync(fs => fs.CompanyId == companyId);
        }

        // Save Facebook settings for a company
        public async Task<bool> SaveFacebookSettingsAsync(int companyId, FacebookSettingsModel facebookSettings)
        {
            var existingSettings = await GetFacebookSettingsAsync(companyId);

            if (existingSettings == null)
            {
                // Create new settings
                facebookSettings.CompanyId = companyId;
                _dbContext.FacebookSettingsModels.Add(facebookSettings);
            }
            else
            {
                // Update existing settings
                existingSettings.AppId = facebookSettings.AppId;
                existingSettings.AccessToken = facebookSettings.AccessToken;
                existingSettings.WebhookVerifyToken = facebookSettings.WebhookVerifyToken;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<FacebookSettingsModel>> GetAllFacebookSettingsAsync()
        {
            return await _dbContext.FacebookSettingsModels.ToListAsync();
        }

        public async Task<FacebookSettingsModel?> GetFacebookSettingsByPageIdAsync(string pageId)
        {
            return await _dbContext.FacebookSettingsModels.FirstOrDefaultAsync(fs => fs.PageId == pageId);
        }

    }
}
