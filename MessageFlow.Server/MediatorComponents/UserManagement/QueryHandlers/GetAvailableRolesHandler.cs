using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers
{
    public class GetAvailableRolesHandler : IRequestHandler<GetAvailableRolesQuery, List<string>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public GetAvailableRolesHandler(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<List<string>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
        {
            return await _roleManager.Roles.Select(r => r.Name).ToListAsync(cancellationToken);
        }
    }
}
