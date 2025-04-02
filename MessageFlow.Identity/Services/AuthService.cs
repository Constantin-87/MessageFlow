//using Microsoft.AspNetCore.Identity;
//using Microsoft.IdentityModel.Tokens;
//using MessageFlow.DataAccess.Models;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using Microsoft.EntityFrameworkCore;
//using System.Security.Cryptography;
//using MessageFlow.Shared.DTOs;

//namespace MessageFlow.Identity.Services
//{
//    public class AuthService
//    {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly SignInManager<ApplicationUser> _signInManager;
//        private readonly IConfiguration _config;
//        private readonly ILogger<AuthService> _logger;

//        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config, ILogger<AuthService> logger)
//        {
//            _userManager = userManager;
//            _signInManager = signInManager;
//            _config = config;
//            _logger = logger;
//        }

//        //// ✅ Login User & Generate JWT Token
//        //public async Task<(bool success, string token, string refreshToken, string errorMessage, ApplicationUserDTO? user)> LoginAsync(string username, string password)
//        //{
//        //    var user = await _userManager.Users
//        //        .Include(u => u.Company) // ✅ Ensure Company is loaded
//        //        .FirstOrDefaultAsync(u => u.UserName == username);
//        //    if (user == null || !await _userManager.CheckPasswordAsync(user, password))
//        //        return (false, string.Empty, string.Empty, "Invalid username or password.", null);

//        //    user.LastActivity = DateTime.UtcNow;
//        //    await _userManager.UpdateAsync(user);

//        //    var token = await GenerateJwtToken(user);
//        //    var refreshToken = await SetRefreshTokenAsync(user);

//        //    var userDto = new ApplicationUserDTO
//        //    {
//        //        Id = user.Id,
//        //        UserName = user.UserName,
//        //        Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
//        //        CompanyId = user.CompanyId,
//        //        CompanyDTO = user.Company != null ? new CompanyDTO
//        //        {
//        //            Id = user.Company.Id,
//        //            CompanyName = user.Company.CompanyName
//        //        } : null
//        //    };

//        //    return (true, token, refreshToken, string.Empty, userDto);
//        //}

//        // ✅ Generate JWT Token
//        private async Task<string> GenerateJwtToken(ApplicationUser user)
//        {
//            var key = Encoding.UTF8.GetBytes(_config["JsonWebToken-Key"] ?? throw new InvalidOperationException("JWT Key is missing."));
//            var roles = await _userManager.GetRolesAsync(user);
//            var role = roles.FirstOrDefault() ?? "Agent";

//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.NameIdentifier, user.Id),
//                new Claim(ClaimTypes.Name, user.UserName),
//                new Claim("CompanyId", user.CompanyId ?? ""),
//                new Claim("CompanyName", user.Company?.CompanyName ?? ""),
//                new Claim("LockoutEnabled", user.LockoutEnabled.ToString()),
//                new Claim("LastActivity", user.LastActivity.ToString("o"))
//            };

//            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

//            var token = new JwtSecurityToken(
//                expires: DateTime.UtcNow.AddMinutes(30),
//                claims: claims,
//                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

//            return new JwtSecurityTokenHandler().WriteToken(token);
//        }

//        // ✅ Logout User
//        //public async Task<bool> LogoutAsync(ClaimsPrincipal userClaims)
//        //{
//        //    var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
//        //    if (string.IsNullOrEmpty(userId))
//        //        return false;

//        //    var user = await _userManager.FindByIdAsync(userId);
//        //    if (user == null)
//        //        return false;

//        //    // ✅ Revoke refresh token
//        //    user.RefreshToken = null;
//        //    user.RefreshTokenExpiryTime = DateTime.MinValue;

//        //    var updateResult = await _userManager.UpdateAsync(user);
//        //    if (!updateResult.Succeeded)
//        //        return false;

//        //    return true;
//        //}

//        //public async Task<bool> UpdateLastActivityAsync(string userId)
//        //{
//        //    var user = await _userManager.FindByIdAsync(userId);
//        //    if (user == null)
//        //        return false;

//        //    user.LastActivity = DateTime.UtcNow;
//        //    var result = await _userManager.UpdateAsync(user);

//        //    return result.Succeeded;
//        //}

//        //// ✅ Validate Session
//        //public async Task<(bool success, ApplicationUser? user)> ValidateSessionAsync(ClaimsPrincipal userClaims)
//        //{
//        //    var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
//        //    if (string.IsNullOrEmpty(userId))
//        //        return (false, null);

//        //    var user = await _userManager.FindByIdAsync(userId);
//        //    if (user == null)
//        //        return (false, null);
//        //    if (user.LastActivity < DateTime.UtcNow.AddMinutes(-15))
//        //    {
//        //        return (false, null);
//        //    }
//        //    return (true, user);
//        //}

//        private string GenerateRefreshToken()
//        {
//            var randomBytes = new byte[64];
//            using var rng = RandomNumberGenerator.Create();
//            rng.GetBytes(randomBytes);
//            return Convert.ToBase64String(randomBytes);
//        }

//        private async Task<string> SetRefreshTokenAsync(ApplicationUser user)
//        {
//            var refreshToken = GenerateRefreshToken();
//            user.RefreshToken = refreshToken;
//            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days validity

//            await _userManager.UpdateAsync(user);
//            return refreshToken;
//        }

//        //public async Task<(bool success, string newAccessToken, string newRefreshToken, string errorMessage)> RefreshTokenAsync(string accessToken, string refreshToken)
//        //{
//        //    var principal = GetPrincipalFromExpiredToken(accessToken);
//        //    if (principal == null)
//        //        return (false, string.Empty, string.Empty, "Invalid access token");

//        //    var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
//        //    var user = await _userManager.FindByIdAsync(userId);

//        //    if (user == null)
//        //        return (false, string.Empty, string.Empty, "User not found.");

//        //    if (user.LastActivity < DateTime.UtcNow.AddMinutes(-15))
//        //        return (false, string.Empty, string.Empty, "Session expired due to inactivity.");

//        //    if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
//        //        return (false, string.Empty, string.Empty, "Invalid refresh token");

//        //    var newAccessToken = await GenerateJwtToken(user);
//        //    var newRefreshToken = GenerateRefreshToken();

//        //    user.RefreshToken = newRefreshToken;
//        //    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

//        //    await _userManager.UpdateAsync(user);

//        //    return (true, newAccessToken, newRefreshToken, "");
//        //}

//        public async Task<bool> RevokeRefreshTokenAsync(string userId)
//        {
//            var user = await _userManager.FindByIdAsync(userId);
//            if (user == null)
//                return false;

//            user.RefreshToken = null;
//            user.RefreshTokenExpiryTime = DateTime.MinValue;

//            await _userManager.UpdateAsync(user);
//            return true;
//        }

//        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
//        {
//            var tokenValidationParameters = new TokenValidationParameters
//            {
//                ValidateAudience = false,
//                ValidateIssuer = false,
//                ValidateIssuerSigningKey = true,
//                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JsonWebToken-Key"]!)),
//                ValidateLifetime = false // Ignore expiration
//            };

//            var tokenHandler = new JwtSecurityTokenHandler();
//            try
//            {
//                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
//                var jwtSecurityToken = securityToken as JwtSecurityToken;

//                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
//                    throw new SecurityTokenException("Invalid token");

//                return principal;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error validating expired token.");
//                return null;
//            }
//        }

//        public async Task<ApplicationUserDTO?> GetUserDtoByIdAsync(string userId)
//        {
//            var user = await _userManager.Users
//                .Include(u => u.Company)
//                .FirstOrDefaultAsync(u => u.Id == userId);

//            if (user == null)
//                return null;

//            return new ApplicationUserDTO
//            {
//                Id = user.Id,
//                UserName = user.UserName,
//                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
//                CompanyId = user.CompanyId,
//                CompanyDTO = user.Company != null ? new CompanyDTO
//                {
//                    Id = user.Company.Id,
//                    CompanyName = user.Company.CompanyName
//                } : null,
//                LastActivity = user.LastActivity
//            };
//        }

//    }
//}
