using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class WhatsAppSettingsRepository : GenericRepository<WhatsAppSettingsModel>, IWhatsAppSettingsRepository
    {

        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public WhatsAppSettingsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public WhatsAppSettingsRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<WhatsAppSettingsModel?> GetSettingsByCompanyIdAsync(string companyId)
        {
            return await _context.WhatsAppSettingsModels
                .Include(ws => ws.PhoneNumbers)
                .FirstOrDefaultAsync(ws => ws.CompanyId == companyId);
        }

        public async Task<List<WhatsAppSettingsModel>> GetAllSettingsAsync()
        {
            return await _context.WhatsAppSettingsModels.Include(ws => ws.PhoneNumbers).ToListAsync();
        }

        public async Task<WhatsAppSettingsModel?> GetSettingsByBusinessAccountIdAsync(string businessAccountId)
        {
            return await _context.WhatsAppSettingsModels
                .Include(ws => ws.PhoneNumbers)
                .FirstOrDefaultAsync(ws => ws.BusinessAccountId == businessAccountId);
        }

    }
}
