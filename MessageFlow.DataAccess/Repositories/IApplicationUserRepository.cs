using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IApplicationUserRepository
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<List<ApplicationUser>> GetUsersForCompanyAsync(string companyId);
        Task<List<ApplicationUser>> GetListOfEntitiesByIdStringAsync(IEnumerable<string> ids);
        Task UpdateEntityAsync(ApplicationUser user);
        Task<bool> DeleteUserByIdAsync(string userId);
        Task<int> CountUsersByCompanyAsync(string companyId);
        Task<List<ApplicationUser>> GetUsersWithCompanyAsync(string currentUserCompanyId, bool isSuperAdmin);
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);

        // Fetch roles for multiple users in a single batch query
        Task<Dictionary<string, List<string>>> GetRolesForUsersAsync(List<string> userIds);
        Task<string?> GetUserCompanyIdAsync(string userId);
    }
}
