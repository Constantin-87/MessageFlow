using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MediatR;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers
{
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, List<ApplicationUserDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<GetAllUsersHandler> _logger;

        public GetAllUsersHandler(
            UserManager<ApplicationUser>
            userManager, IMapper mapper,
            IAuthorizationHelper auth,
            ILogger<GetAllUsersHandler> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _auth = auth;
            _logger = logger;
        }

        public async Task<List<ApplicationUserDTO>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var (_, companyId, isSuperAdmin, errorMessage) = await _auth.CompanyAccess(string.Empty);

            List<ApplicationUser> users;

            if (isSuperAdmin)
            {
                users = await _userManager.Users
                    .Include(u => u.Company)
                    .Include(u => u.Teams)
                    .ToListAsync(cancellationToken);
            }
            else if (!string.IsNullOrEmpty(companyId))
            {
                users = await _userManager.Users
                    .Where(u => u.CompanyId == companyId)
                    .Include(u => u.Company)
                    .Include(u => u.Teams)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning("Unauthorized or unidentified user attempting to access user list.");
                return new();
            }

            var userDtos = _mapper.Map<List<ApplicationUserDTO>>(users);

            foreach (var userDto in userDtos)
            {
                var user = await _userManager.FindByIdAsync(userDto.Id);
                var roles = await _userManager.GetRolesAsync(user);
                userDto.Role = roles.FirstOrDefault() ?? "N/A";
            }

            return userDtos;
        }
    }
}
