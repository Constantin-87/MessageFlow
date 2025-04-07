using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatorComponents.Commands;
using MessageFlow.Identity.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;

namespace MessageFlow.Identity.MediatorComponents.CommandHandlers
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, (bool, string, string, string)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _config = config;
            _tokenService = tokenService;
        }

        public async Task<(bool, string, string, string)> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return (false, "", "", "Invalid access token");

            var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "", "", "User not found");

            if (user.LastActivity < DateTime.UtcNow.AddMinutes(-15))
                return (false, "", "", "Session expired due to inactivity");

            if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return (false, "", "", "Invalid refresh token");

            var newAccessToken = await _tokenService.GenerateJwtTokenAsync(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return (true, newAccessToken, newRefreshToken, "");
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var validationParams = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JsonWebToken-Key"]!)),
                ValidateLifetime = false
            };

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(token, validationParams, out var securityToken);
                if (securityToken is not JwtSecurityToken jwt || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
