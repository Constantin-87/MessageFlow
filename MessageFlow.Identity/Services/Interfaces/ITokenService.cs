using MessageFlow.DataAccess.Models;

namespace MessageFlow.Identity.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        string GenerateRefreshToken();
        Task<string> SetRefreshTokenAsync(ApplicationUser user);
    }
}