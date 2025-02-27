using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public CompanyRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        //// ✅ Constructor for factory usage (parallel-safe operations)
        //public CompanyRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<Company?> GetCompanyWithDetailsByIdAsync(string companyId)
        {
            if (_context != null)
            {
                // ✅ Using the direct ApplicationDbContext (UnitOfWork scenario)
                return await _context.Companies
                    .Include(c => c.Users)              // Include related users
                    .Include(c => c.CompanyEmails)      // Include related emails
                    .Include(c => c.CompanyPhoneNumbers) // Include related phone numbers
                    .FirstOrDefaultAsync(c => c.Id == companyId); // Find by company ID
            }

            if (_dbContextFactory != null)
            {
                // ✅ Using a fresh context from the factory (parallel-safe)
                return await _dbContextFactory.ExecuteScopedAsync(async context =>
                    await context.Companies
                        .Include(c => c.Users)
                        .Include(c => c.CompanyEmails)
                        .Include(c => c.CompanyPhoneNumbers)
                        .FirstOrDefaultAsync(c => c.Id == companyId)
                );
            }

            throw new InvalidOperationException("No available context. Ensure either ApplicationDbContext or IDbContextFactoryService is provided.");
        }


        public async Task<List<Company>> GetAllCompaniesWithUserCountAsync()
        {
            if (_context != null)
            {
                // ✅ Direct context: suitable for UnitOfWork scenarios
                return await _context.Companies
                    .Include(c => c.Users) // Ensure related Users are loaded
                    .Select(company => new Company
                    {
                        Id = company.Id,
                        AccountNumber = company.AccountNumber,
                        CompanyName = company.CompanyName,
                        TotalUsers = company.Users.Count // Calculate user count efficiently
                    })
                    .ToListAsync();
            }

            if (_dbContextFactory != null)
            {
                // ✅ Factory context: fresh context per call (parallel-safe)
                return await _dbContextFactory.ExecuteScopedAsync(async context =>
                    await context.Companies
                        .Include(c => c.Users) // Load users for the count
                        .Select(company => new Company
                        {
                            Id = company.Id,
                            AccountNumber = company.AccountNumber,
                            CompanyName = company.CompanyName,
                            TotalUsers = company.Users.Count
                        })
                        .ToListAsync()
                );
            }

            throw new InvalidOperationException("No available context. Ensure either ApplicationDbContext or IDbContextFactoryService is provided.");
        }
    }
}
