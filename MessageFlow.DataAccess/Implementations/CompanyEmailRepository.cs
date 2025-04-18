using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class CompanyEmailRepository : GenericRepository<CompanyEmail>, ICompanyEmailRepository
    {
        private readonly ApplicationDbContext? _context;

        public CompanyEmailRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<CompanyEmail>> GetCompanyEmailsByCompanyIdAsync(string companyId)
        {
            return await _context.CompanyEmails
                .Where(e => e.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task UpdateEmailsAsync(string companyId, List<CompanyEmail> companyEmails)
        {
            // Get existing emails for the company
            var existingEmails = await _context.CompanyEmails
                .Where(e => e.CompanyId == companyId)
                .ToListAsync();

            // Remove emails that are no longer in the new list
            var emailsToRemove = existingEmails
                .Where(e => !companyEmails.Any(ne => ne.Id == e.Id))
                .ToList();
            _context.CompanyEmails.RemoveRange(emailsToRemove);

            // Add or update emails
            foreach (var email in companyEmails)
            {
                var existingEmail = existingEmails.FirstOrDefault(e => e.Id == email.Id);
                if (existingEmail != null)
                {
                    _context.Entry(existingEmail).CurrentValues.SetValues(email);
                }
                else
                {
                    await _context.CompanyEmails.AddAsync(email);
                }
            }
        }
    }
}
