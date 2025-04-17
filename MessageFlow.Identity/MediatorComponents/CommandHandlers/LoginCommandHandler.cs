using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatorComponents.Commands;
using MessageFlow.Identity.Services.Interfaces;
using MessageFlow.Shared.DTOs;
using MediatR;

namespace MessageFlow.Identity.MediatorComponents.CommandHandlers
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, (bool, string, string, string, ApplicationUserDTO?)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<(bool, string, string, string, ApplicationUserDTO?)> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return (false, "", "", "Invalid username or password.", null);

            user.LastActivity = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var jwt = await _tokenService.GenerateJwtTokenAsync(user);
            var refresh = await _tokenService.SetRefreshTokenAsync(user);

            var userDto = new ApplicationUserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
                CompanyId = user.CompanyId,
                CompanyDTO = user.Company != null
                    ? new CompanyDTO { Id = user.Company.Id, CompanyName = user.Company.CompanyName }
                    : null
            };

            return (true, jwt, refresh, "", userDto);
        }
    }
}
