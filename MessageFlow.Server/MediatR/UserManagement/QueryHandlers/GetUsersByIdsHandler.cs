using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MediatR;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatR.UserManagement.QueryHandlers
{
    public class GetUsersByIdsHandler : IRequestHandler<GetUsersByIdsQuery, List<ApplicationUserDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUsersByIdsHandler> _logger;
        private readonly IAuthorizationHelper _auth;

        public GetUsersByIdsHandler(
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            IAuthorizationHelper auth,
            ILogger<GetUsersByIdsHandler> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _auth = auth;
            _logger = logger;
        }

        public async Task<List<ApplicationUserDTO>> Handle(GetUsersByIdsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userManager.Users
                    .Include(u => u.Company)
                    .Where(u => request.UserIds.Contains(u.Id))
                    .ToListAsync(cancellationToken);

                var authorizedUserDtos = new List<ApplicationUserDTO>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var (isAuthorized, _) = await _auth.UserManagementAccess(user.CompanyId, roles.ToList());

                    if (!isAuthorized)
                    {
                        _logger.LogWarning("Skipping unauthorized access to user {UserId}", user.Id);
                        continue;
                    }

                    var dto = _mapper.Map<ApplicationUserDTO>(user);
                    dto.Role = roles.FirstOrDefault() ?? "N/A";
                    authorizedUserDtos.Add(dto);
                }

                return authorizedUserDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users.");
                return new List<ApplicationUserDTO>();
            }
        }
    }
}