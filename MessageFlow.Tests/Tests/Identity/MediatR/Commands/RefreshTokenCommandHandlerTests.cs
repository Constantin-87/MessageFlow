using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();

    private readonly RefreshTokenCommandHandler _handler;
    private readonly string _jwtKey = "this-is-a-test-key-for-jwt-token";

    public RefreshTokenCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

        _configMock.Setup(c => c["JsonWebToken-Key"]).Returns(_jwtKey);

        _handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object,
            _configMock.Object,
            _tokenServiceMock.Object);
    }

    private string GenerateExpiredToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtKey);

        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }),
            NotBefore = now.AddMinutes(-10),
            Expires = now.AddMinutes(-5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokens()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            RefreshToken = "old-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1),
            LastActivity = DateTime.UtcNow
        };

        var expiredAccessToken = GenerateExpiredToken(user.Id);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(t => t.GenerateJwtTokenAsync(user)).ReturnsAsync("new-jwt");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

        var command = new RefreshTokenCommand(expiredAccessToken, "old-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Item1);
        Assert.Equal("new-jwt", result.Item2);
        Assert.Equal("new-refresh", result.Item3);
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ReturnsFalse()
    {
        var cmd = new RefreshTokenCommand("invalid-token", "irrelevant");
        var result = await _handler.Handle(cmd, default);

        Assert.False(result.Item1);
        Assert.Equal("Invalid access token", result.Item4);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var expiredToken = GenerateExpiredToken("userX");
        _userManagerMock.Setup(x => x.FindByIdAsync("userX")).ReturnsAsync((ApplicationUser?)null);

        var cmd = new RefreshTokenCommand(expiredToken, "any");
        var result = await _handler.Handle(cmd, default);

        Assert.False(result.Item1);
        Assert.Equal("User not found", result.Item4);
    }

    [Fact]
    public async Task Handle_ExpiredSession_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user2",
            LastActivity = DateTime.UtcNow.AddHours(-1)
        };

        var expiredToken = GenerateExpiredToken("user2");
        _userManagerMock.Setup(x => x.FindByIdAsync("user2")).ReturnsAsync(user);

        var cmd = new RefreshTokenCommand(expiredToken, "any");
        var result = await _handler.Handle(cmd, default);

        Assert.False(result.Item1);
        Assert.Equal("Session expired due to inactivity", result.Item4);
    }

    [Fact]
    public async Task Handle_MismatchedRefreshToken_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user3",
            LastActivity = DateTime.UtcNow,
            RefreshToken = "correct",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        var expiredToken = GenerateExpiredToken("user3");
        _userManagerMock.Setup(x => x.FindByIdAsync("user3")).ReturnsAsync(user);

        var cmd = new RefreshTokenCommand(expiredToken, "wrong");
        var result = await _handler.Handle(cmd, default);

        Assert.False(result.Item1);
        Assert.Equal("Invalid refresh token", result.Item4);
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user4",
            LastActivity = DateTime.UtcNow,
            RefreshToken = "expired",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1)
        };

        var expiredToken = GenerateExpiredToken("user4");
        _userManagerMock.Setup(x => x.FindByIdAsync("user4")).ReturnsAsync(user);

        var cmd = new RefreshTokenCommand(expiredToken, "expired");
        var result = await _handler.Handle(cmd, default);

        Assert.False(result.Item1);
        Assert.Equal("Invalid refresh token", result.Item4);
    }
}