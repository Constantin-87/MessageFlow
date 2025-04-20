using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using MediatR;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers
{
    public class DeleteUsersByCompanyHandler : IRequestHandler<DeleteUsersByCompanyCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthorizationHelper _authHelper;
        private readonly ILogger<DeleteUsersByCompanyHandler> _logger;

        public DeleteUsersByCompanyHandler(
            UserManager<ApplicationUser> userManager,
            IAuthorizationHelper authHelper,
            ILogger<DeleteUsersByCompanyHandler> logger)
        {
            _userManager = userManager;
            _authHelper = authHelper;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteUsersByCompanyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userManager.Users
                    .Where(u => u.CompanyId == request.CompanyId)
                    .ToListAsync(cancellationToken);

                if (!users.Any())
                {
                    _logger.LogInformation($"No users found for company ID {request.CompanyId}.");
                    return true;
                }

                var allRoles = new List<string>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    allRoles.AddRange(roles);
                }

                var (isAuthorized, errorMessage) =
                    await _authHelper.UserManagementAccess(request.CompanyId, allRoles.Distinct().ToList());

                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized attempt to delete users: {Error}", errorMessage);
                    return false;
                }

                foreach (var user in users)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("Failed to delete user {UserId}: {Errors}",
                            user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                _logger.LogInformation($"All users in company ID {request.CompanyId} deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting users for company {request.CompanyId}.");
                return false;
            }
        }
    }
}
