using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers
{
    public class GetUsersForCompanyHandler : IRequestHandler<GetUsersForCompanyQuery, List<ApplicationUserDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUsersForCompanyHandler> _logger;

        public GetUsersForCompanyHandler(UserManager<ApplicationUser> userManager, IMapper mapper, ILogger<GetUsersForCompanyHandler> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<ApplicationUserDTO>> Handle(GetUsersForCompanyQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userManager.Users
                    .Include(u => u.Company)
                    .Where(u => u.CompanyId == request.CompanyId)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching users for company {request.CompanyId}.");
                return new List<ApplicationUserDTO>();
            }
        }
    }
}
