using Microsoft.AspNetCore.Identity;
using MessageFlow.Models; 
using Microsoft.EntityFrameworkCore;
using MessageFlow.Components.Accounts.Services;

public class UserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TeamsManagementService _teamsManagementService;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        RoleManager<IdentityRole> roleManager,
        TeamsManagementService teamsManagementService,
        ILogger<UserManagementService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _userStore = userStore;
        _roleManager = roleManager;
        _teamsManagementService = teamsManagementService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    // Create a new user with the specified password, role, and teams
    public async Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUser applicationUser, string password, string selectedRole)
    {
        try
        {
            // Get the current logged-in user
            var creator = await GetCurrentUserAsync();
            if (creator == null)
            {
                return (false, "Creator not found or session expired.");
            }

            var creatorRoles = await _userManager.GetRolesAsync(creator);
            var isSuperAdmin = creatorRoles.Contains("SuperAdmin");

            if (selectedRole == "SuperAdmin")
            {
                if (!isSuperAdmin)
                {
                    return (false, "Only SuperAdmins can assign the SuperAdmin role.");
                }

                if (applicationUser.CompanyId != 6) // Assuming MessageFlow has ID 6
                {
                    return (false, "The SuperAdmin role can only be assigned to users in the MessageFlow Company.");
                }
            }

            if (!isSuperAdmin)
            {
                // Ensure the creator can only create users within their own company
                if (applicationUser.CompanyId != creator.CompanyId)
                {
                    return (false, "You cannot create users for other companies.");
                }

                // Ensure only certain roles can be assigned by non-SuperAdmins
                var allowedRoles = new[] { "Admin", "Manager", "Agent" };
                if (!allowedRoles.Contains(selectedRole))
                {
                    return (false, $"You are not authorized to assign the role: {selectedRole}.");
                }
            }

            // Set username and email for the new user
            await _userStore.SetUserNameAsync(applicationUser, applicationUser.UserName, CancellationToken.None);
            var emailStore = (IUserEmailStore<ApplicationUser>)_userStore;
            await emailStore.SetEmailAsync(applicationUser, applicationUser.Email, CancellationToken.None);

            // Create the new user with the specified password
            var result = await _userManager.CreateAsync(applicationUser, password);
            if (!result.Succeeded)
            {
                _logger.LogError("Error creating user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Assign role ensuring only one role is allowed
            if (!string.IsNullOrEmpty(selectedRole))
            {
                // Remove any roles just in case (though new users shouldn't have roles yet)
                var currentRoles = await _userManager.GetRolesAsync(applicationUser);
                foreach (var role in currentRoles)
                {
                    await _userManager.RemoveFromRoleAsync(applicationUser, role);
                }

                // Assign the new role
                var roleResult = await _userManager.AddToRoleAsync(applicationUser, selectedRole);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Error assigning role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return (false, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            _logger.LogInformation($"User {applicationUser.UserName} created successfully.");
            return (true, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a user.");
            return (false, "An error occurred while creating the user.");
        }
    }

    // Update an existing user with new details, password, role, and teams
    public async Task<(bool success, string errorMessage)> UpdateUserAsync(ApplicationUser applicationUser, string? newPassword, string selectedRole)
    {
        try
        {
            // Get the current logged-in user
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return (false, "Current user not found or session expired.");
            }

            // Get the roles of the current user
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var isSuperAdmin = currentUserRoles.Contains("SuperAdmin");

            // Check if the current user has permission to update the target user
            if (!isSuperAdmin && currentUser.CompanyId != applicationUser.CompanyId)
            {
                _logger.LogWarning($"User {currentUser.UserName} attempted to update a user from a different company.");
                return (false, "You cannot update users for other companies.");
            }

            // Set username and email for the user
            await _userStore.SetUserNameAsync(applicationUser, applicationUser.UserName, CancellationToken.None);
            var emailStore = (IUserEmailStore<ApplicationUser>)_userStore;
            await emailStore.SetEmailAsync(applicationUser, applicationUser.Email, CancellationToken.None);

            // Update the user
            var result = await _userManager.UpdateAsync(applicationUser);
            if (!result.Succeeded)
            {
                _logger.LogError("Error updating user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(applicationUser);
                var passwordResult = await _userManager.ResetPasswordAsync(applicationUser, token, newPassword);
                if (!passwordResult.Succeeded)
                {
                    _logger.LogError("Error updating password: {Errors}", string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    return (false, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                }
            }

            // Ensure the user can only have one role
            if (!string.IsNullOrEmpty(selectedRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(applicationUser);

                // Remove all existing roles
                foreach (var role in currentRoles)
                {
                    var removeRoleResult = await _userManager.RemoveFromRoleAsync(applicationUser, role);
                    if (!removeRoleResult.Succeeded)
                    {
                        _logger.LogError("Error removing role {Role}: {Errors}", role, string.Join(", ", removeRoleResult.Errors.Select(e => e.Description)));
                        return (false, string.Join(", ", removeRoleResult.Errors.Select(e => e.Description)));
                    }
                }

                // Assign the new role
                var roleResult = await _userManager.AddToRoleAsync(applicationUser, selectedRole);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Error assigning role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return (false, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            _logger.LogInformation($"User {applicationUser.UserName} updated successfully.");
            return (true, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the user.");
            return (false, "An error occurred while updating the user.");
        }
    }

    // Delete a user
    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            // Get the current logged-in user
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found or session expired.");
                return false;
            }

            // Find the user to be deleted
            var userToDelete = await _userManager.FindByIdAsync(userId);
            if (userToDelete == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return false;
            }

            // Get the roles of the current user
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var isSuperAdmin = currentUserRoles.Contains("SuperAdmin");

            // Check if the current user has permission to delete the target user
            if (!isSuperAdmin && currentUser.CompanyId != userToDelete.CompanyId)
            {
                _logger.LogWarning($"User {currentUser.UserName} attempted to delete a user from a different company.");
                return false;
            }

            await _teamsManagementService.RemoveUserFromAllTeamsAsync(userId);

            // Delete the user
            var result = await _userManager.DeleteAsync(userToDelete);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User {userToDelete.UserName} deleted successfully.");
                return true;
            }
            else
            {
                _logger.LogWarning($"Failed to delete user {userToDelete.UserName}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the user.");
            return false;
        }
    }

    // Fetch roles for a specific user
    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            return await _userManager.GetRolesAsync(user) as List<string>;
        }
        return new List<string>();
    }    

    // Fetch all available roles
    public async Task<List<string>> GetAvailableRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name).ToListAsync();
    }   

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                _logger.LogWarning("HttpContext or User is null. Unable to fetch the current user.");
                return null;
            }
            return await _userManager.GetUserAsync(httpContext.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching the current user.");
            return null;
        }
    }
}
