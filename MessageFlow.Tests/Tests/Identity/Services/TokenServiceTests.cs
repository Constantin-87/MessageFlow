using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace MessageFlow.Tests.Tests.Identity.Services;

public class TokenServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(x => x["JsonWebToken-Key"]).Returns("very_secret_key_1234567890");

        _tokenService = new TokenService(_userManagerMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_ReturnsValidToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            CompanyId = "company123",
            Company = new Company { CompanyName = "TestCo" },
            LockoutEnabled = true,
            LastActivity = DateTime.UtcNow
        };

        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
        _configMock.Setup(x => x["JsonWebToken-Key"]).Returns("super_secret_key_1234567890_ABCDEFG");

        // Act
        var token = await _tokenService.GenerateJwtTokenAsync(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Decode and inspect
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user1");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == "CompanyName" && c.Value == "TestCo");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsValidBase64String()
    {
        var token = _tokenService.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_UpdatesUserAndReturnsToken()
    {
        var user = new ApplicationUser();

        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var token = await _tokenService.SetRefreshTokenAsync(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(token, user.RefreshToken);
        Assert.True(user.RefreshTokenExpiryTime > DateTime.UtcNow);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_WhenUpdateFails_ThrowsException()
    {
        var user = new ApplicationUser();
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        var ex = await Assert.ThrowsAsync<Exception>(() => _tokenService.SetRefreshTokenAsync(user));
        Assert.Equal("Failed to update user with new refresh token.", ex.Message);
    }
}