using Microsoft.AspNetCore.Identity;
using MessageFlow.Data;
using MessageFlow.Models; // Assuming ApplicationUser and other models are defined here
using Microsoft.EntityFrameworkCore;

public class UserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _userStore = userStore;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    // Create a new user with the specified password, role, and teams
    public async Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUser applicationUser, string password, List<int> selectedTeams, string selectedRole)
    {
        try
        {
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

            // Assign role
            if (!string.IsNullOrEmpty(selectedRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(applicationUser);
                if (!currentRoles.Contains(selectedRole))
                {
                    await _userManager.AddToRoleAsync(applicationUser, selectedRole);
                }
            }

            // Assign teams
            await AssignTeams(applicationUser.Id, selectedTeams);

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
    public async Task<(bool success, string errorMessage)> UpdateUserAsync(ApplicationUser applicationUser, string? newPassword, List<int> selectedTeams, string selectedRole)
    {
        try
        {
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

            // Update role
            if (!string.IsNullOrEmpty(selectedRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(applicationUser);
                if (!currentRoles.Contains(selectedRole))
                {
                    await _userManager.AddToRoleAsync(applicationUser, selectedRole);
                }
            }

            // Update teams
            await AssignTeams(applicationUser.Id, selectedTeams);

            _logger.LogInformation($"User {applicationUser.UserName} updated successfully.");
            return (true, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the user.");
            return (false, "An error occurred while updating the user.");
        }
    }

    // Assign teams to a user
    private async Task AssignTeams(string userId, List<int> selectedTeams)
    {
        var existingUserTeams = _dbContext.UserTeams.Where(ut => ut.UserId == userId).ToList();
        _dbContext.UserTeams.RemoveRange(existingUserTeams);  // Remove old teams

        foreach (var teamId in selectedTeams)
        {
            _dbContext.UserTeams.Add(new UserTeam { UserId = userId, TeamId = teamId });
        }

        await _dbContext.SaveChangesAsync();
    }

    // Delete a user
    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var userToDelete = await _userManager.FindByIdAsync(userId);
            if (userToDelete != null)
            {
                var result = await _userManager.DeleteAsync(userToDelete);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {userToDelete.UserName} deleted.");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to delete user {userToDelete.UserName}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                _logger.LogWarning($"User with ID {userId} not found.");
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

    // Fetch teams for a specific user
    public async Task<List<int>> GetUserTeamsAsync(string userId)
    {
        return await _dbContext.UserTeams
                               .Where(ut => ut.UserId == userId)
                               .Select(ut => ut.TeamId)
                               .ToListAsync();
    }

    // Fetch all available roles
    public async Task<List<string>> GetAvailableRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name).ToListAsync();
    }

    // Fetch teams for a given company
    public async Task<List<Team>> GetTeamsForCompanyAsync(int companyId)
    {
        return await _dbContext.Teams.Where(t => t.CompanyId == companyId).ToListAsync();
    }
}
