using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ITeamRepository
    {
        Task<Team?> GetTeamByIdAsync(string teamId);
        Task<List<Team>> GetTeamsByCompanyIdAsync(string companyId);
        Task<List<Team>> GetTeamsByUserIdAsync(string userId);
        Task<List<ApplicationUser>> GetUsersByTeamIdAsync(string teamId);
        Task AddEntityAsync(Team team);
        Task UpdateEntityAsync(Team Team);
        Task RemoveEntityAsync(Team team);
        void DeleteTeams(List<Team> teams);
        Task RemoveUserFromAllTeamsAsync(string userId);
    }
}