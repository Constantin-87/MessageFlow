using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, ApplicationUserDTO?>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public GetUserByIdHandler(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ApplicationUserDTO?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return null;

            var userDto = _mapper.Map<ApplicationUserDTO>(user);
            var roles = await _userManager.GetRolesAsync(user);
            userDto.Role = roles.FirstOrDefault() ?? "N/A";

            return userDto;
        }
    }
}
