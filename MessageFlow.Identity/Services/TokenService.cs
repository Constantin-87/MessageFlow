using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MessageFlow.Identity.Services
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public TokenService(UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var key = Encoding.UTF8.GetBytes(_config["JsonWebToken-Key"]!);
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("CompanyId", user.CompanyId ?? ""),
                new Claim("CompanyName", user.Company?.CompanyName ?? ""),
                new Claim("LockoutEnabled", user.LockoutEnabled.ToString()),
                new Claim("LastActivity", user.LastActivity.ToString("o"))
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(30),
                claims: claims,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public async Task<string> SetRefreshTokenAsync(ApplicationUser user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to update user with new refresh token.");
            }

            return refreshToken;
        }
    }
}