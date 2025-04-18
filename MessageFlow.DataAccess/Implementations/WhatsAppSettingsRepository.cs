using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class WhatsAppSettingsRepository : GenericRepository<WhatsAppSettingsModel>, IWhatsAppSettingsRepository
    {

        private readonly ApplicationDbContext? _context;

        public WhatsAppSettingsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<WhatsAppSettingsModel?> GetSettingsByCompanyIdAsync(string companyId)
        {
            return await _context.WhatsAppSettingsModels
                .Include(ws => ws.PhoneNumbers)
                .FirstOrDefaultAsync(ws => ws.CompanyId == companyId);
        }

        public async Task<WhatsAppSettingsModel?> GetSettingsByBusinessAccountIdAsync(string businessAccountId)
        {
            return await _context.WhatsAppSettingsModels
                .Include(ws => ws.PhoneNumbers)
                .FirstOrDefaultAsync(ws => ws.BusinessAccountId == businessAccountId);
        }
    }
}
