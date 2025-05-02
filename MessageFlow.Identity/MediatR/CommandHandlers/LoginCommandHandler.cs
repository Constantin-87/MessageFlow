using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.Services.Interfaces;
using MessageFlow.Shared.DTOs;
using MediatR;
using MessageFlow.Identity.Models;

namespace MessageFlow.Identity.MediatR.CommandHandlers
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDTO>
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

        public async Task<LoginResultDTO> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.UserName == request.LoginData.Username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: user '{Username}' not found", request.LoginData.Username);
                return new LoginResultDTO {Success = false, ErrorMessage = "Invalid username or password."};
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login attempt for locked-out user '{Username}'", request.LoginData.Username);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                return new LoginResultDTO{Success = false, ErrorMessage = "Account is locked.", LockoutEnd = lockoutEnd};
            }

            if (!await _userManager.CheckPasswordAsync(user, request.LoginData.Password))
            {
                await _userManager.AccessFailedAsync(user);
                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                _logger.LogWarning("Login failed: invalid password for user '{Username}'. Failed attempts: {Attempts}", request.LoginData.Username, failedAttempts);
                return new LoginResultDTO{Success = false, ErrorMessage = "Invalid username or password."};
            }
            user.LastActivity = DateTime.UtcNow;
            await _userManager.ResetAccessFailedCountAsync(user);
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
            _logger.LogInformation("User '{Username}' authenticated successfully", user.UserName);
            return new LoginResultDTO
            {
                Success = true,
                Token = jwt,
                RefreshToken = refresh,
                ErrorMessage = "",
                User = userDto
            };
        }
    }
}