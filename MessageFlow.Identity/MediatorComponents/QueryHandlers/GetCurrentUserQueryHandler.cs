using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatorComponents.Queries;
using MessageFlow.Shared.DTOs;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Identity.MediatorComponents.QueryHandlers
{
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApplicationUserDTO?>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ApplicationUserDTO?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return null;

            return new ApplicationUserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
                CompanyId = user.CompanyId,
                CompanyDTO = user.Company != null
                    ? new CompanyDTO { Id = user.Company.Id, CompanyName = user.Company.CompanyName }
                    : null,
                LastActivity = user.LastActivity
            };
        }
    }
}
