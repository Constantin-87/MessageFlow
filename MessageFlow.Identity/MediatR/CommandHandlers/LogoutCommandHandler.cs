using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.Commands;
using System.Security.Claims;
using MediatR;

namespace MessageFlow.Identity.MediatR.CommandHandlers
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public LogoutCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}