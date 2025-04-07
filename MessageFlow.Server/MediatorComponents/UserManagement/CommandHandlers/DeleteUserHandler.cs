using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using MediatR;

namespace MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers
{
    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DeleteUserHandler> _logger;

        public DeleteUserHandler(UserManager<ApplicationUser> userManager, ILogger<DeleteUserHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
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
    }
}
