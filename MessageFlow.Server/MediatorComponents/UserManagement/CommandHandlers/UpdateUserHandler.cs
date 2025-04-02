using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers
{
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, (bool success, string errorMessage)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateUserHandler> _logger;

        public UpdateUserHandler(UserManager<ApplicationUser> userManager, IMapper mapper, ILogger<UpdateUserHandler> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = request.UserDto;
                var targetUser = await _userManager.FindByIdAsync(dto.Id);
                if (targetUser == null) return (false, "Target user not found.");

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
