using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.Commands;
using MediatR;

namespace MessageFlow.Identity.MediatR.CommandHandlers
{
    public class UpdateLastActivityCommandHandler : IRequestHandler<UpdateLastActivityCommand, bool>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UpdateLastActivityCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> Handle(UpdateLastActivityCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return false;

            user.LastActivity = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }
    }
}