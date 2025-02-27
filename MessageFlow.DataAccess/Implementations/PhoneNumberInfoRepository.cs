using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class PhoneNumberInfoRepository : GenericRepository<PhoneNumberInfo>, IPhoneNumberInfoRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public PhoneNumberInfoRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public PhoneNumberInfoRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<PhoneNumberInfo?> GetPhoneNumberByIdAsync(int id)
        {
            return await _context.PhoneNumberInfo
                .Include(p => p.WhatsAppSettings)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<PhoneNumberInfo>> GetPhoneNumbersByWhatsAppSettingsAsync(string settingsId)
        {
            return await _context.PhoneNumberInfo
                .Where(p => p.WhatsAppSettingsModelId == settingsId)
                .ToListAsync();
        }

    }
}
