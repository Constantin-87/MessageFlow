using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MessageFlow.Tests.Helpers
{
    public static class JwtTestHelper
    {
        private const string SecretKey = "THIS_IS_A_FAKE_TEST_KEY_1234567890";
        private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes(SecretKey));

        public static string GenerateTestJwt(string role, string userId = "test-user", DateTime? expires = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, role)
            };

            var now = DateTime.UtcNow;
            var expiry = expires ?? now.AddHours(1);

            // Ensure NotBefore is before Expiry
            var notBefore = expiry < now ? expiry.AddSeconds(-1) : now;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = notBefore,
                IssuedAt = notBefore,
                Expires = expiry,
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


    }
}