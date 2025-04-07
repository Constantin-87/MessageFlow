using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using MediatR;

namespace MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers
{
    public class DeleteUsersByCompanyHandler : IRequestHandler<DeleteUsersByCompanyCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DeleteUsersByCompanyHandler> _logger;

        public DeleteUsersByCompanyHandler(UserManager<ApplicationUser> userManager, ILogger<DeleteUsersByCompanyHandler> logger)
        {
            _userManager = userManager;
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

                foreach (var user in users)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning($"Failed to delete user {user.Id} in company {request.CompanyId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
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
