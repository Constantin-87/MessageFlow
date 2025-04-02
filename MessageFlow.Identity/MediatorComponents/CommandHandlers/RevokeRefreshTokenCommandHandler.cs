using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatorComponents.Commands;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Identity.MediatorComponents.CommandHandlers
{
    public class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RevokeRefreshTokenCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }
    }
}
