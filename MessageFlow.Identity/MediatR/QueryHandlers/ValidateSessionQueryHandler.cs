using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.Queries;
using System.Security.Claims;
using MediatR;

namespace MessageFlow.Identity.MediatR.QueryHandlers
{
    public class ValidateSessionQueryHandler : IRequestHandler<ValidateSessionQuery, (bool, ApplicationUser?)>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ValidateSessionQueryHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<(bool, ApplicationUser?)> Handle(ValidateSessionQuery request, CancellationToken cancellationToken)
        {
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return (false, null);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, null);

            if (user.LastActivity < DateTime.UtcNow.AddMinutes(-15))
                return (false, null);

            return (true, user);
        }
    }
}