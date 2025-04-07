using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using MediatR;

namespace MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, (bool success, string errorMessage)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateUserHandler> _logger;

        public CreateUserHandler(UserManager<ApplicationUser> userManager, IMapper mapper, ILogger<CreateUserHandler> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var applicationUser = _mapper.Map<ApplicationUser>(request.UserDto);
                applicationUser.Id = Guid.NewGuid().ToString();

                var result = await _userManager.CreateAsync(applicationUser, request.UserDto.NewPassword);
                if (!result.Succeeded)
                {
                    return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var roleResult = await _userManager.AddToRoleAsync(applicationUser, request.UserDto.Role);
                if (!roleResult.Succeeded)
                {
                    return (false, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                return (true, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a user.");
                return (false, "An error occurred while creating the user.");
            }
        }
    }
}
