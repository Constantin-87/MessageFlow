using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MediatR;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatR.UserManagement.QueryHandlers
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, ApplicationUserDTO?>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<GetUserByIdHandler> _logger;

        public GetUserByIdHandler(
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            IAuthorizationHelper auth,
            ILogger<GetUserByIdHandler> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _auth = auth;
            _logger = logger;
        }

        public async Task<ApplicationUserDTO?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var (isAuthorized, errorMessage) = await _auth.UserManagementAccess(user.CompanyId, roles.ToList());
            if (!isAuthorized)
            {
                _logger.LogWarning("Unauthorized user detail access: {Error}", errorMessage);
                return null;
            }

            var userDto = _mapper.Map<ApplicationUserDTO>(user);
            userDto.Role = roles.FirstOrDefault() ?? "N/A";

            return userDto;
        }
    }
}