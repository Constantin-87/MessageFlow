using MessageFlow.Server.Data;
using MessageFlow.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Server.Components.Accounts.Services
{
    public class TeamsManagementService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TeamsManagementService> _logger;

        public TeamsManagementService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<TeamsManagementService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        // Method to retrieve users for a specific team by team ID
        public async Task<List<ApplicationUser>> GetUsersForTeamAsync(int teamId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            return await dbContext.Users
                .Where(u => u.UserTeams.Any(ut => ut.TeamId == teamId))
                .ToListAsync();
        }

        // Method to count total users in a company (across all teams)
        public async Task<int> GetTotalUsersForCompanyAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            return await dbContext.Users
                .Where(u => u.CompanyId == companyId)
                .CountAsync();
        }

        // Fetch teams for a specific user
        public async Task<List<(int TeamId, string TeamName)>> GetUserTeamsAsync(string userId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();

            var userTeams = await dbContext.UserTeams
                .Where(ut => ut.UserId == userId)
                .Select(ut => new { ut.Team.Id, ut.Team.TeamName }) // Anonymous type
                .ToListAsync();

            // Convert to tuples after the query
            return userTeams.Select(t => (t.Id, t.TeamName)).ToList();
        }



        // Fetch teams for a given company
        public async Task<List<Team>> GetTeamsForCompanyAsync(int companyId)
        {
            await using var dbContext = _contextFactory.CreateDbContext();
            return await dbContext.Teams
                .Where(t => t.CompanyId == companyId)
                .Include(t => t.UserTeams)
                    .ThenInclude(ut => ut.User) // Include the User in each UserTeam
                .ToListAsync();
        }

        // Add a new team to a company
        public async Task<(bool success, string errorMessage)> AddTeamToCompanyAsync(int companyId, string teamName, string teamDescription, List<ApplicationUser> assignedUsers)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == companyId);
                if (!companyExists)
                {
                    return (false, "Company not found.");
                }

                var team = new Team
                {
                    TeamName = teamName,
                    CompanyId = companyId,
                    TeamDescription = teamDescription,
                    UserTeams = assignedUsers.Select(user => new UserTeam { UserId = user.Id }).ToList()
                };

                dbContext.Teams.Add(team);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Team {teamName} added successfully to company {companyId}.");
                return (true, "Team added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding team");
                return (false, "An error occurred while adding the team.");
            }
        }

        // Delete a specific team by its ID
        public async Task<(bool success, string errorMessage)> DeleteTeamByIdAsync(int teamId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var team = await dbContext.Teams.FindAsync(teamId);
                if (team == null)
                {
                    return (false, "Team not found.");
                }

                dbContext.Teams.Remove(team);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Team with ID {teamId} deleted successfully.");
                return (true, "Team deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting team with ID {teamId}");
                return (false, "An error occurred while deleting the team.");
            }
        }

        // Delete all teams associated with a specific company ID
        public async Task DeleteTeamsByCompanyIdAsync(int companyId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var teams = await dbContext.Teams.Where(t => t.CompanyId == companyId).ToListAsync();
                if (teams.Any())
                {
                    dbContext.Teams.RemoveRange(teams);
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation($"All teams for company {companyId} deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting teams for company {companyId}");
                throw;
            }
        }

        // Remove a user from all teams
        public async Task RemoveUserFromAllTeamsAsync(string userId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var userTeams = await dbContext.UserTeams.Where(ut => ut.UserId == userId).ToListAsync();
                if (userTeams.Any())
                {
                    dbContext.UserTeams.RemoveRange(userTeams);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"All team associations for user {userId} have been removed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing user {userId} from all teams.");
                throw;
            }
        }

        public async Task AssignUserToTeamAsync(string userId, int teamId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var exists = await dbContext.UserTeams.AnyAsync(ut => ut.UserId == userId && ut.TeamId == teamId);
                if (!exists)
                {
                    dbContext.UserTeams.Add(new UserTeam { UserId = userId, TeamId = teamId });
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"User {userId} assigned to team {teamId} successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning user {userId} to team {teamId}");
                throw;
            }
        }

        public async Task RemoveUserFromTeamAsync(string userId, int teamId)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var userTeam = await dbContext.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId);
                if (userTeam != null)
                {
                    dbContext.UserTeams.Remove(userTeam);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"User {userId} removed from team {teamId} successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing user {userId} from team {teamId}");
                throw;
            }
        }

        public async Task<(bool success, string errorMessage)> UpdateTeamAsync(Team team, List<ApplicationUser> assignedUsers)
        {
            try
            {
                await using var dbContext = _contextFactory.CreateDbContext();

                var existingTeam = await dbContext.Teams
                    .Include(t => t.UserTeams)
                    .FirstOrDefaultAsync(t => t.Id == team.Id);

                if (existingTeam == null)
                {
                    return (false, "Team not found.");
                }

                existingTeam.TeamName = team.TeamName;
                existingTeam.TeamDescription = team.TeamDescription;

                // Update user assignments
                dbContext.UserTeams.RemoveRange(existingTeam.UserTeams);
                existingTeam.UserTeams = assignedUsers.Select(user => new UserTeam { UserId = user.Id }).ToList();

                await dbContext.SaveChangesAsync();

                return (true, "Team updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team");
                return (false, "An error occurred while updating the team.");
            }
        }




    }
}
