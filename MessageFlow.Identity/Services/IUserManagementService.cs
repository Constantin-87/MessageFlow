using MessageFlow.Shared.DTOs;

namespace MessageFlow.Identity.Services
{
    public interface IUserManagementService
    {
        Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUserDTO applicationUser);
        Task<(bool success, string errorMessage)> UpdateUserAsync(ApplicationUserDTO applicationUser);
        Task<bool> DeleteUserAsync(string userId);
        //Task<List<string>> GetRoleForUserAsync(string userId);
        Task<List<string>> GetAvailableRolesAsync();
        Task<List<ApplicationUserDTO>> GetUsersAsync();
        Task<ApplicationUserDTO?> GetUserByIdAsync(string userId);
        Task<bool> DeleteUsersByCompanyIdAsync(string companyId);
        Task<List<ApplicationUserDTO>> GetUsersForCompanyAsync(string companyId);

    }
}