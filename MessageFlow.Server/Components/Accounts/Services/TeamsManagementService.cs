using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using MessageFlow.DataAccess.Models;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace MessageFlow.Server.Components.Accounts.Services
{
    public class TeamsManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TeamsManagementService> _logger;
        private readonly IMapper _mapper;

        public TeamsManagementService(IUnitOfWork unitOfWork, ILogger<TeamsManagementService> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        // Method to retrieve users for a specific team by team ID
        public async Task<List<ApplicationUserDTO>> GetUsersForTeamAsync(string teamId)
        {
            var users = await _unitOfWork.Teams.GetUsersByTeamIdAsync(teamId);
            return _mapper.Map<List<ApplicationUserDTO>>(users);
        }

        // ✅ Count total users in a company (across all teams)
        public async Task<int> GetTotalUsersForCompanyAsync(string companyId)
        {
            return await _unitOfWork.ApplicationUsers.CountUsersByCompanyAsync(companyId);
        }

        // Fetch teams for a specific user
        public async Task<List<TeamDTO>> GetUserTeamsAsync(string userId)
        {
            var userTeams = await _unitOfWork.Teams.GetTeamsByUserIdAsync(userId);
            return _mapper.Map<List<TeamDTO>>(userTeams);
        }

        // ✅ Fetch teams for a given company
        public async Task<List<TeamDTO>> GetTeamsForCompanyAsync(string companyId)
        {
            var teams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(companyId);
            return _mapper.Map<List<TeamDTO>>(teams);
        }

        // Add a new team to a company
        public async Task<(bool success, string errorMessage)> AddTeamToCompanyAsync(string companyId, string teamName, string teamDescription, List<ApplicationUserDTO> assignedUsers)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found.");
                }

                // ✅ Fetch existing ApplicationUser entities by their IDs
                var userIds = assignedUsers.Select(user => user.Id).ToList();
                var existingUsers = await _unitOfWork.ApplicationUsers.GetListOfEntitiesByIdStringAsync(userIds);

                var team = new Team
                {
                    Id = Guid.NewGuid().ToString(),
                    TeamName = teamName,
                    CompanyId = companyId,
                    TeamDescription = teamDescription,
                    Users = existingUsers
                };

                await _unitOfWork.Teams.AddEntityAsync(team);
                await _unitOfWork.SaveChangesAsync();

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
        public async Task<(bool success, string errorMessage)> DeleteTeamByIdAsync(string teamId)
        {
            try
            {
                var team = await _unitOfWork.Teams.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    return (false, "Team not found.");
                }

                // ✅ Remove the team using UnitOfWork
                _unitOfWork.Teams.RemoveEntityAsync(team);

                // ✅ Save changes
                await _unitOfWork.SaveChangesAsync();

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
        public async Task DeleteTeamsByCompanyIdAsync(string companyId)
        {
            try
            {
                var teams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(companyId);

                if (teams.Any())
                {
                    // ✅ Remove all teams using UnitOfWork
                    _unitOfWork.Teams.DeleteTeams(teams);

                    // ✅ Save changes
                    await _unitOfWork.SaveChangesAsync();

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
                // ✅ Remove user from all teams using UnitOfWork
                await _unitOfWork.Teams.RemoveUserFromAllTeamsAsync(userId);

                // ✅ Save changes
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"All team associations for user {userId} have been removed.");
                               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing user {userId} from all teams.");
                throw;
            }
        }

      
        
        public async Task<(bool success, string errorMessage)> UpdateTeamAsync(TeamDTO teamDto)
        {
            try
            {
                // ✅ Fetch the existing team from the database (tracked by EF)
                var existingTeam = await _unitOfWork.Teams.GetTeamByIdAsync(teamDto.Id);

                if (existingTeam == null)
                    return (false, "Team not found.");

                // ✅ Update team details
                existingTeam.TeamName = teamDto.TeamName;
                existingTeam.TeamDescription = teamDto.TeamDescription;

                // ✅ Clear existing users to prevent duplicate tracking issues
                existingTeam.Users.Clear();

                if (teamDto.UsersDTO?.Any() == true)
                {
                    var userIds = teamDto.UsersDTO.Select(u => u.Id).ToList();
                    var users = await _unitOfWork.ApplicationUsers.GetListOfEntitiesByIdStringAsync(userIds); // Efficient batch fetch

                    // ✅ Add the fetched users (EF tracks these properly)
                    foreach (var user in users)
                    {
                        existingTeam.Users.Add(user);
                    }
                }

                // ✅ Use inherited UpdateEntityAsync to track the entity
                await _unitOfWork.Teams.UpdateEntityAsync(existingTeam);

                // ✅ Save changes
                await _unitOfWork.SaveChangesAsync();

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
