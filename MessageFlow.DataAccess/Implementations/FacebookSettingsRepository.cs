using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class FacebookSettingsRepository : GenericRepository<FacebookSettingsModel>, IFacebookSettingsRepository
    {

        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public FacebookSettingsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public FacebookSettingsRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

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
