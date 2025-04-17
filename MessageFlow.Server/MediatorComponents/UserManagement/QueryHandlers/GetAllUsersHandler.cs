using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MediatR;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;

namespace MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers
{
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, List<ApplicationUserDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public GetAllUsersHandler(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<List<ApplicationUserDTO>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userManager.Users
                .Include(u => u.Company)
                .Include(u => u.Teams)
                .ToListAsync(cancellationToken);

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
