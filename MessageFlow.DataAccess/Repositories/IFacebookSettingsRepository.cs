﻿using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IFacebookSettingsRepository
    {
        Task<FacebookSettingsModel?> GetSettingsByCompanyIdAsync(string companyId);
        Task<FacebookSettingsModel?> GetSettingsByPageIdAsync(string pageId);
        Task AddEntityAsync(FacebookSettingsModel settings);
        Task UpdateEntityAsync(FacebookSettingsModel settings);
    }
}