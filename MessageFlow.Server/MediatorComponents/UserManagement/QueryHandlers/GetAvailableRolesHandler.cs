using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MediatR;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers
{
    public class GetAvailableRolesHandler : IRequestHandler<GetAvailableRolesQuery, List<string>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuthorizationHelper _auth;

        public GetAvailableRolesHandler(RoleManager<IdentityRole> roleManager, IAuthorizationHelper auth)
        {
            _roleManager = roleManager;
            _auth = auth;
        }

        public async Task<List<string>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
        {
            var (_, _, isSuperAdmin, _) = await _auth.CompanyAccess(string.Empty);

            var allRoles = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);

            return isSuperAdmin
                ? allRoles
                : allRoles.Where(r => !string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
