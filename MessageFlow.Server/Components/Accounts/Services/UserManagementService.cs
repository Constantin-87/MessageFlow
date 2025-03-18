//using Microsoft.EntityFrameworkCore;
//using MessageFlow.DataAccess.Models;
//using MessageFlow.Server.Components.Accounts.Services;
//using MessageFlow.Shared.DTOs;
//using AutoMapper;
//using System.Security.Claims;
//using MessageFlow.DataAccess.Services;

//public class UserManagementService : IUserManagementService
//{
//    private readonly TeamsManagementService _teamsManagementService;
//    private readonly ILogger<UserManagementService> _logger;
//    private readonly IMapper _mapper;
//    private readonly ClaimsPrincipal _currentUser;

//    private readonly IUnitOfWork _unitOfWork;

//    public UserManagementService(
//        IUnitOfWork unitOfWork,
//        TeamsManagementService teamsManagementService,
//        ILogger<UserManagementService> logger,
//        IHttpContextAccessor httpContextAccessor,
//        IMapper mapper)
//    {
//        _unitOfWork = unitOfWork;
//        _teamsManagementService = teamsManagementService;
//        _logger = logger;
//        _currentUser = httpContextAccessor.HttpContext?.User ?? throw new ArgumentNullException(nameof(httpContextAccessor));
//        _mapper = mapper;
//    }

//    // Create a new user with the specified password and role
//    public async Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUserDTO applicationUserDTO, string password)
//    {
//        try
//        {
//            // ✅ Use new helper for role validation
//            var (allowed, error) = await ValidatePermissionsAsync(applicationUserDTO.CompanyId, "create", applicationUserDTO.Role);
//            if (!allowed) return (false, error);


//            var applicationUser = _mapper.Map<ApplicationUser>(applicationUserDTO);

//            if (string.IsNullOrWhiteSpace(applicationUser.Id))
//            {
//                applicationUser.Id = Guid.NewGuid().ToString();
//                _logger.LogInformation("Generated new GUID for ApplicationUser.Id: {UserId}", applicationUser.Id);
//            }

//            // ✅ Retrieve the Company entity
//            var company = await _unitOfWork.Companies.GetByIdStringAsync(applicationUserDTO.CompanyId);
//            if (company == null)
//            {
//                _logger.LogError("Company with ID {CompanyId} not found.", applicationUserDTO.CompanyId);
//                return (false, "Selected company does not exist.");
//            }
//            applicationUser.Company = company;

//            // 1️⃣ Create user
//            var (createSuccess, createError) = await _unitOfWork.ApplicationUsers.CreateUserAsync(applicationUser, password);
//            if (!createSuccess)
//            {
//                _logger.LogError("Failed to create user {UserName}: {Error}", applicationUser.UserName, createError);
//                return (false, createError);
//            }

//            // 2️⃣ Update email
//            var (emailSuccess, emailError) = await _unitOfWork.ApplicationUsers.UpdateEmailAsync(applicationUser, applicationUser.Email);
//            if (!emailSuccess)
//            {
//                _logger.LogError("Failed to update email for user {UserName}: {Error}", applicationUser.UserName, emailError);
//                return (false, emailError);
//            }

//            // 3️⃣ Assign role
//            var (roleSuccess, assignRoleError) = await _unitOfWork.ApplicationUsers.AssignRoleAsync(applicationUser, applicationUserDTO.Role);
//            if (!roleSuccess)
//            {
//                _logger.LogError("Failed to assign role {Role} to user {UserName}: {Error}", applicationUserDTO.Role, applicationUser.UserName, assignRoleError);
//                return (false, assignRoleError);
//            }
            

//            _logger.LogInformation($"User {applicationUser.UserName} created successfully.");
//            return (true, "User created successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "An error occurred while creating a user.");
//            return (false, "An error occurred while creating the user.");
//        }
//    }

//    // Update an existing user with new details, password, and role
//    public async Task<(bool success, string errorMessage)> UpdateUserAsync(ApplicationUserDTO applicationUserDTO, string? newPassword)
//    {
//        try
//        {
//            var (allowed, error) = await ValidatePermissionsAsync(applicationUserDTO.CompanyId, "update", applicationUserDTO.Role);
//            if (!allowed) return (false, error);

//            var targetUser = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(applicationUserDTO.Id);
//            if (targetUser == null) return (false, "Target user not found.");

//            // ✅ Update email
//            var (emailSuccess, emailError) = await _unitOfWork.ApplicationUsers.UpdateEmailAsync(targetUser, applicationUserDTO.UserEmail);
//            if (!emailSuccess)
//            {
//                _logger.LogError("Failed to update email for user {UserName}: {Error}", applicationUserDTO.UserName, emailError);
//                return (false, emailError);
//            }

//            // ✅ Update password if provided
//            if (!string.IsNullOrWhiteSpace(newPassword))
//            {
//                var (passwordSuccess, passwordError) = await _unitOfWork.ApplicationUsers.UpdatePasswordAsync(targetUser, newPassword);
//                if (!passwordSuccess)
//                {
//                    _logger.LogError("Password update failed for {UserName}: {Error}", applicationUserDTO.UserName, passwordError);
//                    return (false, passwordError);
//                }
//            }

//            // ✅ Handle role update
//            var (removeSuccess, removeError) = await _unitOfWork.ApplicationUsers.RemoveUserRolesAsync(targetUser);
//            if (!removeSuccess)
//            {
//                _logger.LogError("Failed to remove roles for {UserName}: {Error}", applicationUserDTO.UserName, removeError);
//                return (false, removeError);
//            }
            
//            var (assignSuccess, assignError) = await _unitOfWork.ApplicationUsers.AssignRoleAsync(targetUser, applicationUserDTO.Role);
//            if (!assignSuccess)
//            {
//                _logger.LogError("Failed to assign role {Role} to user {UserName}: {Error}", applicationUserDTO.Role, applicationUserDTO.UserName, assignError);
//                return (false, assignError);
//            }            

//            _logger.LogInformation("User {UserName} updated successfully.", applicationUserDTO.UserName);
//            return (true, "User updated successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "An error occurred while updating the user.");
//            return (false, "An error occurred while updating the user.");
//        }
//    }

//    // Delete a user
//    public async Task<bool> DeleteUserAsync(string userId)
//    {
//        try
//        {
//            // Get the current logged-in user
//            var currentUser = await GetCurrentUserAsync();
//            if (currentUser == null)
//            {
//                _logger.LogWarning("Current user not found or session expired.");
//                return false;
//            }

//            var targetUser = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(userId);
//            if (targetUser == null) return false;


//            var applicationUserDTO = _mapper.Map<ApplicationUserDTO>(targetUser);
            

//            var (allowed, error) = await ValidatePermissionsAsync(applicationUserDTO.CompanyId, "create", null);
//            if (!allowed) return (false);

//            await _teamsManagementService.RemoveUserFromAllTeamsAsync(userId);

//            var deleteSuccess = await _unitOfWork.ApplicationUsers.DeleteUserByIdAsync(userId);
//            if (deleteSuccess)
//            {
//                _logger.LogInformation("User {UserName} deleted successfully.", targetUser.UserName);
//                return true;
//            }

//            _logger.LogWarning("Failed to delete user {UserName}.", targetUser.UserName);
//            return false;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "An error occurred while deleting the user.");
//            return false;
//        }
//    }

//    // ✅ Fetch all available roles using the repository
//    public async Task<List<string>> GetAvailableRolesAsync()
//    {
//        try
//        {
//            // ✅ Get the current user and check if they are a SuperAdmin
//            var currentUser = await GetCurrentUserAsync();
//            if (currentUser == null)
//            {
//                _logger.LogWarning("Current user not found or session expired.");
//                return new List<string>();
//            }

//            var isSuperAdmin = await UserHasRoleAsync(currentUser.Id, "SuperAdmin");

//            // ✅ Fetch all roles from the repository
//            var roles = await _unitOfWork.ApplicationUsers.GetAllRolesAsync();

//            // ✅ Remove "SuperAdmin" role if the user is not a SuperAdmin
//            if (!isSuperAdmin)
//            {
//                roles.Remove("SuperAdmin");
//            }

//            return roles;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "An error occurred while fetching available roles.");
//            return new List<string>();
//        }
//    }

//    public async Task<List<ApplicationUserDTO>> GetUsersAsync()
//    {
//        var currentUser = await GetCurrentUserAsync();
//        if (currentUser == null)
//        {
//            throw new InvalidOperationException("Unable to find the current user.");
//        }

//        var isSuperAdmin = await UserHasRoleAsync(currentUser.Id, "SuperAdmin");

//        // ✅ Fetch users based on role and company
//        var users = await _unitOfWork.ApplicationUsers.GetUsersWithCompanyAsync(currentUser.CompanyId, isSuperAdmin);

//        // ✅ Batch fetch roles (handled via repository)
//        var userIds = users.Select(u => u.Id).ToList();
//        var rolesDict = await _unitOfWork.ApplicationUsers.GetRolesForUsersAsync(userIds);

//        // ✅ Map to DTOs using AutoMapper
//        var userDtos = _mapper.Map<List<ApplicationUserDTO>>(users);

//        // ✅ Assign roles to DTOs
//        userDtos.ForEach(u => u.Role = rolesDict.GetValueOrDefault(u.Id)?.FirstOrDefault() ?? "N/A");

//        return userDtos;
//    }

//    public async Task<ApplicationUserDTO?> GetUserByIdAsync(string userId)
//    {
//        var user = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(userId);
//        if (user == null)
//        {
//            _logger.LogWarning("No user found with ID {UserId}.", userId);
//            return null;
//        }

//        var roles = await _unitOfWork.ApplicationUsers.GetRoleForUserAsync(user.Id);
//        var userDto = _mapper.Map<ApplicationUserDTO>(user);
//        userDto.Role = roles.FirstOrDefault() ?? "N/A";

//        return userDto;
//    }

//    /// <summary>
//    /// Validates if the current user can perform a specific action (create, update, delete) with the given role and target company.
//    /// </summary>
//    private async Task<(bool success, string errorMessage)> ValidatePermissionsAsync(string targetCompanyId, string action, string selectedRole)
//    {
//        var currentUser = await GetCurrentUserAsync();

//        var isSuperAdmin = await UserHasRoleAsync(currentUser.Id, "SuperAdmin");
//        var isAdmin = await UserHasRoleAsync(currentUser.Id, "Admin");

//        // ✅ SuperAdmins can perform any action
//        if (isSuperAdmin)
//        {
//            if (!string.IsNullOrEmpty(selectedRole) && selectedRole == "SuperAdmin" && targetCompanyId != "6")
//                return (false, "SuperAdmin role can only be assigned to users in company with ID 6.");

//            return (true, string.Empty);
//        }

//        // ✅ Admins can only manage users within their own company (excluding SuperAdmin role)
//        if (isAdmin && currentUser.CompanyId == targetCompanyId)
//        {
//            // ⚙️ If no role is provided (e.g., delete action), skip role check
//            if (string.IsNullOrEmpty(selectedRole))
//            {
//                return (true, string.Empty); // ✅ Allowed action without role assignment
//            }

//            var allowedRoles = new[] { "Admin", "AgentManager", "Agent" };
//            if (!allowedRoles.Contains(selectedRole))
//                return (false, $"You cannot assign the role '{selectedRole}'. Allowed roles: Admin, Manager, Agent.");

//            return (true, string.Empty);
//        }

//        return (false, $"You do not have permission to {action} users in this company.");
//    }

//    private async Task<ApplicationUser> GetCurrentUserAsync()
//    {
//        try
//        {
//            var userId = _currentUser?.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrEmpty(userId))
//            {
//                _logger.LogWarning("User ID not found in claims.");
//                return null;
//            }

//            var user = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(userId);
//            if (user == null)
//            {
//                _logger.LogWarning("No user found with ID {UserId}.", userId);
//            }

//            return user;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error occurred while fetching the current user.");
//            return null;
//        }
//    }


//    /// <summary>
//    /// Checks if the user has any of the specified roles.
//    /// </summary>
//    private async Task<bool> UserHasRoleAsync(string userId, params string[] rolesToCheck)
//    {
//        var userRoles = await _unitOfWork.ApplicationUsers.GetRoleForUserAsync(userId);
//        return rolesToCheck.Any(role => userRoles.Contains(role));
//    }

//}
