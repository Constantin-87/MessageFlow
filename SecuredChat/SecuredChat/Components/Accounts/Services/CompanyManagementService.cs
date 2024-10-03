using SecuredChat.Data;
using SecuredChat.Models;
using Microsoft.EntityFrameworkCore;

namespace SecuredChat.Components.Accounts.Services
{
    public class CompanyManagementService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CompanyManagementService> _logger;

        public CompanyManagementService(ApplicationDbContext dbContext, ILogger<CompanyManagementService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Create a new company with its associated teams
        public async Task<(bool success, string errorMessage)> CreateCompanyAsync(Company company)
        {
            try
            {
                _dbContext.Companies.Add(company);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company {company.CompanyName} created successfully.");
                return (true, "Company created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return (false, "An error occurred while creating the company.");
            }
        }

        // Update an existing company and its teams
        public async Task<(bool success, string errorMessage)> UpdateCompanyAsync(Company company)
        {
            try
            {
                _dbContext.Companies.Update(company);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company {company.CompanyName} updated successfully.");
                return (true, "Company updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company");
                return (false, "An error occurred while updating the company.");
            }
        }

        // Fetch a specific company by ID
        public async Task<Company?> GetCompanyByIdAsync(int companyId)
        {
            return await _dbContext.Companies.Include(c => c.Teams).FirstOrDefaultAsync(c => c.Id == companyId);
        }

        // Add a new team to a company
        public async Task<(bool success, string errorMessage)> AddTeamToCompanyAsync(int companyId, string teamName)
        {
            try
            {
                var company = await GetCompanyByIdAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found.");
                }

                company.Teams.Add(new Team { TeamName = teamName });
                await _dbContext.SaveChangesAsync();
                return (true, "Team added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding team");
                return (false, "An error occurred while adding the team.");
            }
        }

        // Fetch all companies with the total number of associated users
        public async Task<List<CompanyInfo>> GetAllCompaniesAsync()
        {
            var companies = await _dbContext.Companies
                .Select(c => new CompanyInfo
                {
                    Id = c.Id,
                    AccountNumber = c.AccountNumber,
                    CompanyName = c.CompanyName,
                    TotalUsers = _dbContext.Users.Count(u => u.CompanyId == c.Id)
                })
                .ToListAsync();

            return companies;
        }
    }    
}
