using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IApplicationUserRepository
    {
        //Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUser user, string password);
        //Task<(bool success, string errorMessage)> UpdateEmailAsync(ApplicationUser user, string newEmail);
        //Task<(bool success, string errorMessage)> UpdatePasswordAsync(ApplicationUser user, string newPassword);

        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<List<ApplicationUser>> GetUsersForCompanyAsync(string companyId);
        Task<List<ApplicationUser>> GetListOfEntitiesByIdStringAsync(IEnumerable<string> ids);
        //Task AddEntityAsync(ApplicationUser user);
        Task UpdateEntityAsync(ApplicationUser user);
        //Task RemoveEntityAsync(ApplicationUser user);
        Task<bool> DeleteUserByIdAsync(string userId);
        Task<int> CountUsersByCompanyAsync(string companyId);
        Task<List<ApplicationUser>> GetUsersWithCompanyAsync(string currentUserCompanyId, bool isSuperAdmin);
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);


        //Task<List<string>> GetAllRolesAsync();
        //Task<List<string>> GetRoleForUserAsync(string userId);
        // 🚀 Fetch roles for multiple users in a single batch query
        Task<Dictionary<string, List<string>>> GetRolesForUsersAsync(List<string> userIds);
        //Task<(bool success, string errorMessage)> AssignRoleAsync(ApplicationUser user, string role);
        //Task<(bool success, string errorMessage)> RemoveUserRolesAsync(ApplicationUser user);


        Task<string?> GetUserCompanyIdAsync(string userId);

    }
}
