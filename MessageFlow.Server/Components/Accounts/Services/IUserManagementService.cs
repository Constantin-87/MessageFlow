//using MessageFlow.Shared.DTOs;

//namespace MessageFlow.Server.Components.Accounts.Services
//{
//    public interface IUserManagementService
//    {
//        Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUserDTO applicationUser, string password);
//        Task<(bool success, string errorMessage)> UpdateUserAsync(ApplicationUserDTO applicationUser, string? newPassword);
//        Task<bool> DeleteUserAsync(string userId);
//        //Task<List<string>> GetRoleForUserAsync(string userId);
//        Task<List<string>> GetAvailableRolesAsync();
//        Task<List<ApplicationUserDTO>> GetUsersAsync();
//        Task<ApplicationUserDTO?> GetUserByIdAsync(string userId);
//    }
//}