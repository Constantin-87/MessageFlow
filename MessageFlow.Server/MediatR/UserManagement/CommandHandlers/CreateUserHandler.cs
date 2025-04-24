using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using MediatR;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatR.UserManagement.CommandHandlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, (bool success, string errorMessage)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateUserHandler> _logger;
        private readonly IAuthorizationHelper _authorizationHelper;

        public CreateUserHandler(
            UserManager<ApplicationUser> userManager,
            IMapper mapper, 
            ILogger<CreateUserHandler> logger,
            IAuthorizationHelper authorizationHelper)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _authorizationHelper = authorizationHelper;
        }

        public async Task<(bool success, string errorMessage)> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userDto = request.UserDto;

                var (isAuthorized, errorMessage) = await _authorizationHelper.UserManagementAccess(
                    userDto.CompanyId,
                    new List<string> { userDto.Role }
                );

                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized user creation attempt: {Message}", errorMessage);
                    return (false, errorMessage);
                }

                var applicationUser = _mapper.Map<ApplicationUser>(userDto);
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