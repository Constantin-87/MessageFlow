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

        public PhoneNumberInfoRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

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
