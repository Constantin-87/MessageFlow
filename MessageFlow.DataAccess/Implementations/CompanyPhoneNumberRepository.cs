using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class CompanyPhoneNumberRepository : GenericRepository<CompanyPhoneNumber>, ICompanyPhoneNumberRepository
    {
        private readonly ApplicationDbContext? _context;

        public CompanyPhoneNumberRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<CompanyPhoneNumber>> GetPhoneNumbersByCompanyAsync(string companyId)
        {
            return await _context.CompanyPhoneNumbers
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task UpdatePhoneNumbersAsync(string companyId, List<CompanyPhoneNumber> companyPhoneNumbers)
        {
            // Get existing phone numbers
            var existingPhoneNumbers = await _context.CompanyPhoneNumbers
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();

            // Remove phone numbers that are no longer in the new list
            var phoneNumbersToRemove = existingPhoneNumbers
                .Where(p => !companyPhoneNumbers.Any(np => np.Id == p.Id))
                .ToList();
            _context.CompanyPhoneNumbers.RemoveRange(phoneNumbersToRemove);

            // Add or update phone numbers
            foreach (var phoneNumber in companyPhoneNumbers)
            {
                var existingPhoneNumber = existingPhoneNumbers.FirstOrDefault(p => p.Id == phoneNumber.Id);
                if (existingPhoneNumber != null)
                {
                    _context.Entry(existingPhoneNumber).CurrentValues.SetValues(phoneNumber);
                }
                else
                {
                    await _context.CompanyPhoneNumbers.AddAsync(phoneNumber);
                }
            }
        }
    }
}
