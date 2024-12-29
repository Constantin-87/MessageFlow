using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Security.Claims;

namespace MessageFlow.Components.Accounts.Services
{
    public class CompanyManagementService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<CompanyManagementService> _logger;
        private readonly ClaimsPrincipal _currentUser;
        private readonly TeamsManagementService _teamsManagementService;

        public CompanyManagementService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<CompanyManagementService> logger,
            IHttpContextAccessor httpContextAccessor,
            TeamsManagementService teamsManagementService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _currentUser = httpContextAccessor.HttpContext?.User ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _teamsManagementService = teamsManagementService ?? throw new ArgumentNullException(nameof(teamsManagementService));
        }

        // Create a new company with its associated teams
        public async Task<(bool success, string errorMessage)> CreateCompanyAsync(Company company)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                if (!await IsSuperAdminAsync())
                {
                    return (false, "Only SuperAdmins can create companies.");
                }

                dbContext.Companies.Add(company);
                await dbContext.SaveChangesAsync();
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
                await using var dbContext = _contextFactory.CreateDbContext();

                var userId = _currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRoles = await dbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                var isSuperAdmin = userRoles.Contains("SuperAdmin");

                if (!isSuperAdmin)
                {
                    // Get the company ID of the current user
                    var userCompanyId = await dbContext.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.CompanyId)
                        .FirstOrDefaultAsync();

                    // make sure the admin is updating their own company only
                    if (company.Id != userCompanyId)
                    {
                        return (false, "You are not authorized to update this company.");
                    }
                }

                    dbContext.Companies.Update(company);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company {company.CompanyName} updated successfully.");
                return (true, "Company updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company");
                return (false, "An error occurred while updating the company.");
            }
        }

        // Delete a company
        public async Task<(bool success, string errorMessage)> DeleteCompanyAsync(int companyId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                if (!await IsSuperAdminAsync())
                {
                    return (false, "Only SuperAdmins can delete companies.");
                }

                var company = await GetCompanyByIdAsync(dbContext, companyId);

                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // Get all users associated with the company
                var users = await dbContext.Users.Where(u => u.CompanyId == companyId).ToListAsync();

                // Remove all related UserRoles, UserClaims, UserLogins, UserTokens for each user
                foreach (var user in users)
                {
                    var userRoles = await dbContext.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
                    dbContext.UserRoles.RemoveRange(userRoles);

                    var userClaims = await dbContext.UserClaims.Where(uc => uc.UserId == user.Id).ToListAsync();
                    dbContext.UserClaims.RemoveRange(userClaims);

                    var userLogins = await dbContext.UserLogins.Where(ul => ul.UserId == user.Id).ToListAsync();
                    dbContext.UserLogins.RemoveRange(userLogins);

                    var userTokens = await dbContext.UserTokens.Where(ut => ut.UserId == user.Id).ToListAsync();
                    dbContext.UserTokens.RemoveRange(userTokens);

                    // Remove the user from the UserTeams relationship
                    var userTeams = await dbContext.UserTeams.Where(ut => ut.UserId == user.Id).ToListAsync();
                    dbContext.UserTeams.RemoveRange(userTeams);

                    // Finally, remove the user
                    dbContext.Users.Remove(user);
                }

                // Delete all teams associated with the company via TeamsManagementService
                await _teamsManagementService.DeleteTeamsByCompanyIdAsync(companyId);

                // Remove the company itself
                dbContext.Companies.Remove(company);

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Company {company.CompanyName} and all associated data deleted successfully.");
                return (true, "Company and all associated data deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company");
                return (false, "An error occurred while deleting the company.");
            }
        }

        // Check if the current user is a SuperAdmin
        private async Task<bool> IsSuperAdminAsync()
        {
            await using var dbContext = _contextFactory.CreateDbContext();

            var userId = _currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
            var roles = await dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            return roles.Contains("SuperAdmin");
        }

        // Fetch a specific company by ID
        public async Task<Company?> GetCompanyByIdAsync(ApplicationDbContext dbContext, int companyId)
        {
            return await dbContext.Companies
                .Include(c => c.Teams)
                    .ThenInclude(t => t.UserTeams)
                    .ThenInclude(ut => ut.User)
                .FirstOrDefaultAsync(c => c.Id == companyId);
        }       

        // Fetch all companies with the total number of associated users
        public async Task<List<Company>> GetAllCompaniesAsync()
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            var companies = await dbContext.Companies
                .Select(c => new Company
                {
                    Id = c.Id,
                    AccountNumber = c.AccountNumber,
                    CompanyName = c.CompanyName,
                })
                .ToListAsync();

            return companies;
        }

        public async Task<string?> GetCompanyNameByIdAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            var company = await dbContext.Companies
                .Where(c => c.Id == companyId)
                .Select(c => c.CompanyName)
                .FirstOrDefaultAsync();

            return company;
        }

        public async Task<Company?> GetCompanyForUserAsync(ClaimsPrincipal user)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            // First, get the company ID of the user
            var companyId = await  dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.CompanyId)
                .FirstOrDefaultAsync();

            if (companyId == 0) // If the companyId is invalid, return null
            {
                return null;
            }

            // Now fetch the actual Company object by its Id
            var company = await GetCompanyByIdAsync(dbContext, companyId);
            return company;
        }

        // Fetch all users for a specific company
        public async Task<List<ApplicationUser>> GetUsersForCompanyAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();

            var users = await dbContext.Users
                .Where(u => u.CompanyId == companyId)
                .ToListAsync();

            return users;
        }

        public ApplicationDbContext CreateDbContext()
        {
            return _contextFactory.CreateDbContext();
        }

    }
}
