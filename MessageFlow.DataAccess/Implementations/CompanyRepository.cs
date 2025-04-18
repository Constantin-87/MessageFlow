using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext? _context;

        public CompanyRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Company?> GetCompanyWithDetailsByIdAsync(string companyId)
        {
            if (_context != null)
            {
                return await _context.Companies
                    .Include(c => c.Users)
                    .Include(c => c.CompanyEmails)
                    .Include(c => c.CompanyPhoneNumbers)
                    .FirstOrDefaultAsync(c => c.Id == companyId);
            }
            throw new InvalidOperationException("No available context. Ensure either ApplicationDbContext or IDbContextFactoryService is provided.");
        }

        public async Task<List<Company>> GetAllCompaniesWithUserCountAsync()
        {
            if (_context != null)
            {
                return await _context.Companies
                    .Include(c => c.Users)
                    .Select(company => new Company
                    {
                        Id = company.Id,
                        CompanyName = company.CompanyName,
                        AccountNumber = company.AccountNumber,
                        Description = company.Description,
                        IndustryType = company.IndustryType,
                        WebsiteUrl = company.WebsiteUrl,
                        TotalUsers = company.Users.Count
                    })
                    .ToListAsync();
            }
            throw new InvalidOperationException("No available context. Ensure either ApplicationDbContext or IDbContextFactoryService is provided.");
        }
    }
}
