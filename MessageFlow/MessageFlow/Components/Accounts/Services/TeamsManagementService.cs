using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Components.Accounts.Services
{
    public class TeamsManagementService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TeamsManagementService> _logger;

        public TeamsManagementService(ApplicationDbContext dbContext, ILogger<TeamsManagementService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Method to retrieve users for a specific team by team ID
        public async Task<List<ApplicationUser>> GetUsersForTeamAsync(int teamId)
        {
            return await _dbContext.Users
                .Where(u => u.UserTeams.Any(ut => ut.TeamId == teamId))
                .ToListAsync();
        }

        // Method to count total users in a company (across all teams)
        public async Task<int> GetTotalUsersForCompanyAsync(int companyId)
        {
            return await _dbContext.Users
                .Where(u => u.CompanyId == companyId)
                .CountAsync();
        }
    }
}
