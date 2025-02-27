using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ITeamRepository
    {
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team?> GetTeamByIdAsync(string teamId);
        Task<List<Team>> GetTeamsByCompanyIdAsync(string companyId);
        Task<List<Team>> GetTeamsByUserIdAsync(string userId); // ✅ Get teams by user ID
        Task<List<ApplicationUser>> GetUsersByTeamIdAsync(string teamId); // ✅ Get users in a team
        Task AddEntityAsync(Team team);
        Task UpdateEntityAsync(Team Team);
        Task RemoveEntityAsync(Team team);
        void DeleteTeams(List<Team> teams);
        Task RemoveUserFromAllTeamsAsync(string userId); // ✅ New Method
        Task AddUserToTeamAsync(string teamId, string userId); // ✅ New Method
        Task RemoveUserFromTeamAsync(string teamId, string userId); // ✅ New Method
    }
}
