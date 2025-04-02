//using MessageFlow.DataAccess.Services;
//using MessageFlow.Shared.DTOs;
//using MessageFlow.DataAccess.Models;
//using AutoMapper;
//using MessageFlow.Server.Authorization;
//using System.ComponentModel.Design;

//namespace MessageFlow.Server.Services
//{
//    public class TeamsManagementService
//    {
//        private readonly HttpClient _identityHttpClient;
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly ILogger<TeamsManagementService> _logger;
//        private readonly IMapper _mapper;
//        private readonly IAuthorizationHelper _authorizationHelper;

//        public TeamsManagementService
//        (
//            IUnitOfWork unitOfWork,
//            ILogger<TeamsManagementService> logger,
//            IMapper mapper,
//            IHttpClientFactory httpClientFactory,
//            IAuthorizationHelper authorizationHelper)
//        {
//            _unitOfWork = unitOfWork;
//            _logger = logger;
//            _mapper = mapper;
//            _identityHttpClient = httpClientFactory.CreateClient("IdentityAPI");
//            _authorizationHelper = authorizationHelper;
//        }

//        // Fetch teams for a given company
//        //public async Task<List<TeamDTO>> GetTeamsForCompanyAsync(string companyId)
//        //{
//        //    var (isAuthorized, errorMessage) = await _authorizationHelper.CanManageTeam(companyId);
//        //    if (!isAuthorized)
//        //    {
//        //        _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
//        //        return new List<TeamDTO>();
//        //    }

//        //    var teams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(companyId);
//        //    return _mapper.Map<List<TeamDTO>>(teams);
//        //}

//        //// Method to retrieve users for a specific team by team ID
//        //public async Task<List<ApplicationUserDTO>> GetUsersForTeamAsync(string teamId)
//        //{
//        //    var team = await _unitOfWork.Teams.GetTeamByIdAsync(teamId);
//        //    if (team == null)
//        //    {
//        //        _logger.LogWarning($"Team with ID {teamId} not found.");
//        //        return new List<ApplicationUserDTO>();
//        //    }
//        //    var (isAuthorized, errorMessage) = await _authorizationHelper.CanManageTeam(team.CompanyId);

//        //    if (!isAuthorized)
//        //    {
//        //        _logger.LogWarning($"Unauthorized access to team users: {errorMessage}");
//        //        return new List<ApplicationUserDTO>();
//        //    }

//        //    var users = await _unitOfWork.Teams.GetUsersByTeamIdAsync(teamId);
//        //    return _mapper.Map<List<ApplicationUserDTO>>(users);
//        //}        
        
//        //// Add a new team to a company
//        //public async Task<(bool success, string errorMessage)> AddTeamToCompanyAsync(TeamDTO teamDto)
//        //{
//        //    try
//        //    {
//        //        var (isAuthorized, errorMessage) = await _authorizationHelper.CanManageTeam(teamDto.CompanyId);
//        //        if (!isAuthorized)
//        //        {
//        //            _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
//        //            return (false, errorMessage);
//        //        }

//        //        List<ApplicationUser> mappedUsers = new();

//        //        if (teamDto.AssignedUserIds != null && teamDto.AssignedUserIds.Any())
//        //        {
//        //            // ✅ Call Identity Service to get user details by IDs
//        //            var response = await _identityHttpClient.PostAsJsonAsync("api/user-management/get-users-by-ids", teamDto.AssignedUserIds);

//        //            if (!response.IsSuccessStatusCode)
//        //            {
//        //                _logger.LogError("Failed to fetch users from Identity Service.");
//        //                return (false, "An error occurred while retrieving the users.");
//        //            }

//        //            var existingUsers = await response.Content.ReadFromJsonAsync<List<ApplicationUserDTO>>();

//        //            if (existingUsers == null)
//        //            {
//        //                _logger.LogError("Identity Service returned null users list.");
//        //                return (false, "An error occurred while retrieving the users.");
//        //            }

//        //            // ✅ Map ApplicationUserDTO to ApplicationUser entities
//        //            mappedUsers = _mapper.Map<List<ApplicationUser>>(existingUsers);
//        //        }

//        //        // ✅ Create and populate the new Team entity
//        //        var team = new Team
//        //        {
//        //            Id = Guid.NewGuid().ToString(),
//        //            TeamName = teamDto.TeamName,
//        //            TeamDescription = teamDto.TeamDescription,
//        //            CompanyId = teamDto.CompanyId,
//        //            Users = mappedUsers
//        //        };

//        //        await _unitOfWork.Teams.AddEntityAsync(team);
//        //        await _unitOfWork.SaveChangesAsync();

//        //        _logger.LogInformation($"Team '{team.TeamName}' added successfully to company {team.CompanyId}.");

//        //        return (true, "Team added successfully.");
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Error adding team");
//        //        return (false, "An error occurred while adding the team.");
//        //    }
//        //}

//        //// Delete a specific team by its ID
//        //public async Task<(bool success, string errorMessage)> DeleteTeamByIdAsync(string teamId)
//        //{
//        //    try
//        //    {
//        //        var team = await _unitOfWork.Teams.GetTeamByIdAsync(teamId);
//        //        if (team == null)
//        //        {
//        //            return (false, "Team not found.");
//        //        }

//        //        var (isAuthorized, errorMessage) = await _authorizationHelper.CanManageTeam(team.CompanyId);
//        //        if (!isAuthorized)
//        //        {
//        //            _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
//        //            return (false, errorMessage);
//        //        }

//        //        // ✅ Remove the team using UnitOfWork
//        //        _unitOfWork.Teams.RemoveEntityAsync(team);

//        //        // ✅ Save changes
//        //        await _unitOfWork.SaveChangesAsync();

//        //        _logger.LogInformation($"Team with ID {teamId} deleted successfully.");
//        //        return (true, "Team deleted successfully.");
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, $"Error deleting team with ID {teamId}");
//        //        return (false, "An error occurred while deleting the team.");
//        //    }
//        //}

//        //// Delete all teams associated with a specific company ID
//        //public async Task DeleteTeamsByCompanyIdAsync(string companyId)
//        //{
//        //    try
//        //    {
//        //        var (isAuthorized, errorMessage) = await _authorizationHelper.CanManageTeam(companyId);
//        //        if (!isAuthorized)
//        //        {
//        //            _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
//        //            return;
//        //        }

//        //        var teams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(companyId);

//        //        if (teams.Any())
//        //        {
//        //            // ✅ Remove all teams using UnitOfWork
//        //            _unitOfWork.Teams.DeleteTeams(teams);

//        //            // ✅ Save changes
//        //            await _unitOfWork.SaveChangesAsync();

//        //            _logger.LogInformation($"All teams for company {companyId} deleted successfully.");
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, $"Error deleting teams for company {companyId}");
//        //        throw;
//        //    }
//        //}

//        public async Task<(bool success, string errorMessage)> UpdateTeamAsync(TeamDTO teamDto)
//        {
//            try
//            {
//                var (isAuthorized, errorMessage) = await _authorizationHelper.CanManageTeam(teamDto.CompanyId);
//                if (!isAuthorized)
//                {
//                    _logger.LogWarning("Unauthorized team update attempt: " + errorMessage);
//                    return (false, errorMessage);
//                }
//                // ✅ Fetch the existing team from the database (tracked by EF)
//                var existingTeam = await _unitOfWork.Teams.GetTeamByIdAsync(teamDto.Id);

//                if (existingTeam == null)
//                    return (false, "Team not found.");

//                // ✅ Update team details
//                existingTeam.TeamName = teamDto.TeamName;
//                existingTeam.TeamDescription = teamDto.TeamDescription;

//                // ✅ Clear existing users to prevent duplicate tracking issues
//                existingTeam.Users.Clear();

//                if (teamDto.AssignedUserIds?.Any() == true)
//                {
//                    var userIds = teamDto.AssignedUserIds;

//                    var trackedUsers = await _unitOfWork.ApplicationUsers.GetListOfEntitiesByIdStringAsync(userIds);

//                    if (trackedUsers == null || !trackedUsers.Any())
//                    {
//                        _logger.LogError("No valid users found for the provided IDs.");
//                        return (false, "No valid users found.");
//                    }

//                    // ✅ Add tracked users to the team
//                    foreach (var user in trackedUsers)
//                    {
//                        existingTeam.Users.Add(user);
//                    }
//                }

//                // ✅ Use inherited UpdateEntityAsync to track the entity
//                await _unitOfWork.Teams.UpdateEntityAsync(existingTeam);

//                // ✅ Save changes
//                await _unitOfWork.SaveChangesAsync();

//                return (true, "Team updated successfully.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating team");
//                return (false, "An error occurred while updating the team.");
//            }
//        }
//    }
//}
