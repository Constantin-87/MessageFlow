using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class FacebookSettingsRepository : GenericRepository<FacebookSettingsModel>, IFacebookSettingsRepository
    {

        private readonly ApplicationDbContext? _context;

        public FacebookSettingsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<FacebookSettingsModel?> GetSettingsByPageIdAsync(string pageId)
        {
            return await _context.FacebookSettingsModels
                .FirstOrDefaultAsync(f => f.PageId == pageId);
        }

        public async Task<FacebookSettingsModel?> GetSettingsByCompanyIdAsync(string companyId)
        {
            return await _context.FacebookSettingsModels
                .FirstOrDefaultAsync(f => f.CompanyId == companyId);
        }
    }
}
