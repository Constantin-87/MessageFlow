using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using MediatR;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatR.UserManagement.CommandHandlers
{
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, (bool success, string errorMessage)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UpdateUserHandler> _logger;
        private readonly IAuthorizationHelper _auth;

        public UpdateUserHandler(
            UserManager<ApplicationUser> userManager,
            ILogger<UpdateUserHandler> logger,
            IAuthorizationHelper auth)
        {
            _userManager = userManager;
            _logger = logger;
            _auth = auth;
        }

        public async Task<(bool success, string errorMessage)> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = request.UserDto;
                var targetUser = await _userManager.FindByIdAsync(dto.Id);
                if (targetUser == null) return (false, "Target user not found.");

                var (isAuthorized, errorMessage) = await _auth.UserManagementAccess(dto.CompanyId, new List<string> { dto.Role });
                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized user update attempt: {Error}", errorMessage);
                    return (false, errorMessage);
                }

                targetUser.UserName = dto.UserName;
                targetUser.Email = dto.UserEmail;
                targetUser.PhoneNumber = dto.PhoneNumber;
                targetUser.LockoutEnabled = dto.LockoutEnabled;

                var updateResult = await _userManager.UpdateAsync(targetUser);
                if (!updateResult.Succeeded)
                {
                    return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }

                if (!string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(targetUser);
                    var passwordResult = await _userManager.ResetPasswordAsync(targetUser, resetToken, dto.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        return (false, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }
                }

                var oldRoles = await _userManager.GetRolesAsync(targetUser);
                await _userManager.RemoveFromRolesAsync(targetUser, oldRoles);
                await _userManager.AddToRoleAsync(targetUser, dto.Role);

                return (true, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user.");
                return (false, "An error occurred while updating the user.");
            }
        }
    }
}