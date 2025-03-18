using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using AutoMapper;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.Identity.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserManagementService> _logger;
        private readonly IMapper _mapper;
        private readonly ClaimsPrincipal _currentUser;

        public UserManagementService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserManagementService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _currentUser = httpContextAccessor.HttpContext?.User ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _mapper = mapper;
        }

        // ✅ Create a new user with the specified password and role
        public async Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUserDTO applicationUserDTO)
        {
            try
            {
                var applicationUser = _mapper.Map<ApplicationUser>(applicationUserDTO);
                applicationUser.Id = Guid.NewGuid().ToString();

                // 1️⃣ Create user
                var result = await _userManager.CreateAsync(applicationUser, applicationUserDTO.NewPassword);
                if (!result.Succeeded)
                {
                    return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                // 2️⃣ Assign role
                var roleResult = await _userManager.AddToRoleAsync(applicationUser, applicationUserDTO.Role);
                if (!roleResult.Succeeded)
                {
                    return (false, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                return (true, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a user.");
                return (false, "An error occurred while creating the user.");
            }
        }

        // ✅ Update user details, password, and role
        public async Task<(bool success, string errorMessage)> UpdateUserAsync(ApplicationUserDTO applicationUserDTO)
        {
            try
            {
                var targetUser = await _userManager.FindByIdAsync(applicationUserDTO.Id);
                if (targetUser == null) return (false, "Target user not found.");

                // ✅ Update username
                targetUser.UserName = applicationUserDTO.UserName;

                // ✅ Update email
                targetUser.Email = applicationUserDTO.UserEmail;

                // ✅ Update phone number
                targetUser.PhoneNumber = applicationUserDTO.PhoneNumber;

                // ✅ Update other Identity fields
                targetUser.LockoutEnabled = applicationUserDTO.LockoutEnabled;

                var updateResult = await _userManager.UpdateAsync(targetUser);
                if (!updateResult.Succeeded)
                {
                    return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }

                // ✅ Update password if provided
                if (!string.IsNullOrWhiteSpace(applicationUserDTO.NewPassword))
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(targetUser);
                    var passwordResult = await _userManager.ResetPasswordAsync(targetUser, resetToken, applicationUserDTO.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        return (false, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }
                }

                // ✅ Update role
                var oldRoles = await _userManager.GetRolesAsync(targetUser);
                await _userManager.RemoveFromRolesAsync(targetUser, oldRoles);
                await _userManager.AddToRoleAsync(targetUser, applicationUserDTO.Role);

                return (true, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user.");
                return (false, "An error occurred while updating the user.");
            }
        }

        // ✅ Delete user
        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the user.");
                return false;
            }
        }

        // ✅ Fetch all available roles
        public async Task<List<string>> GetAvailableRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        }

        // ✅ Fetch all users
        public async Task<List<ApplicationUserDTO>> GetUsersAsync()
        {
            var users = await _userManager.Users.Include(u => u.Company).ToListAsync();
            var userDtos = _mapper.Map<List<ApplicationUserDTO>>(users);

            foreach (var userDto in userDtos)
            {
                var user = await _userManager.FindByIdAsync(userDto.Id);
                var roles = await _userManager.GetRolesAsync(user);
                userDto.Role = roles.FirstOrDefault() ?? "N/A";
            }

            return userDtos;
        }

        // ✅ Fetch user by ID
        public async Task<ApplicationUserDTO?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var userDto = _mapper.Map<ApplicationUserDTO>(user);
            var roles = await _userManager.GetRolesAsync(user);
            userDto.Role = roles.FirstOrDefault() ?? "N/A";

            return userDto;
        }

        public async Task<bool> DeleteUsersByCompanyIdAsync(string companyId)
        {
            try
            {
                // ✅ Fetch all users for the given company
                var users = await _userManager.Users.Where(u => u.CompanyId == companyId).ToListAsync();

                if (!users.Any())
                {
                    _logger.LogInformation($"No users found for company ID {companyId}.");
                    return true; // No users to delete
                }

                // ✅ Delete each user
                foreach (var user in users)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning($"Failed to delete user {user.Id} in company {companyId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                _logger.LogInformation($"All users in company ID {companyId} deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting users for company {companyId}.");
                return false;
            }
        }

        public async Task<List<ApplicationUserDTO>> GetUsersForCompanyAsync(string companyId)
        {
            try
            {
                // Fetch users for the given companyId
                var users = await _userManager.Users
                    .Include(u => u.Company) // Include company if needed in DTO
                    .Where(u => u.CompanyId == companyId)
                    .ToListAsync();

                var userDtos = _mapper.Map<List<ApplicationUserDTO>>(users);

                // Set roles manually for each user
                foreach (var userDto in userDtos)
                {
                    var user = await _userManager.FindByIdAsync(userDto.Id);
                    var roles = await _userManager.GetRolesAsync(user);
                    userDto.Role = roles.FirstOrDefault() ?? "N/A";
                }

                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching users for company {companyId}.");
                return new List<ApplicationUserDTO>();
            }
        }


    }
}
