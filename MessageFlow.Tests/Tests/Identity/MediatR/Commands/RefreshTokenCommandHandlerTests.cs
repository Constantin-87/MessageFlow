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
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly string _jwtKey = "this-is-a-test-key-for-jwt-token";

    public RefreshTokenCommandHandlerTests()
    {
        _configMock.Setup(c => c["JsonWebToken-Key"]).Returns(_jwtKey);
    }

    private RefreshTokenCommandHandler CreateHandler(ApplicationUser user)
    {
        var users = new[] { user }.AsQueryable();
        var userManagerMock = TestDbContextFactory.CreateMockUserManager(users);

        userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        return new RefreshTokenCommandHandler(
            userManagerMock.Object,
            _configMock.Object,
            _tokenServiceMock.Object
        );
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

        var handler = CreateHandler(user);
        var expiredToken = GenerateExpiredToken(user.Id);

        _tokenServiceMock.Setup(t => t.GenerateJwtTokenAsync(user)).ReturnsAsync("new-jwt");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

        var result = await handler.Handle(new RefreshTokenCommand(expiredToken, "old-token"), default);

        Assert.True(result.Item1);
        Assert.Equal("new-jwt", result.Item2);
        Assert.Equal("new-refresh", result.Item3);
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ReturnsFalse()
    {
        var handler = CreateHandler(new ApplicationUser { Id = "unused" });

        var result = await handler.Handle(new RefreshTokenCommand("invalid-token", "irrelevant"), default);

        Assert.False(result.Item1);
        Assert.Equal("Invalid access token", result.Item4);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var token = GenerateExpiredToken("userX");

        var userManagerMock = TestDbContextFactory.CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable());
        var handler = new RefreshTokenCommandHandler(userManagerMock.Object, _configMock.Object, _tokenServiceMock.Object);

        var result = await handler.Handle(new RefreshTokenCommand(token, "any"), default);

        Assert.False(result.Item1);
        Assert.Equal("User not found", result.Item4);
    }

    [Fact]
    public async Task Handle_ExpiredSession_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user2",
            RefreshToken = "token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1),
            LastActivity = DateTime.UtcNow.AddHours(-2)
        };

        var handler = CreateHandler(user);
        var token = GenerateExpiredToken(user.Id);

        var result = await handler.Handle(new RefreshTokenCommand(token, "token"), default);

        Assert.False(result.Item1);
        Assert.Equal("Session expired due to inactivity", result.Item4);
    }

    [Fact]
    public async Task Handle_MismatchedRefreshToken_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user3",
            RefreshToken = "correct-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1),
            LastActivity = DateTime.UtcNow
        };

        var handler = CreateHandler(user);
        var token = GenerateExpiredToken(user.Id);

        var result = await handler.Handle(new RefreshTokenCommand(token, "wrong-token"), default);

        Assert.False(result.Item1);
        Assert.Equal("Invalid refresh token", result.Item4);
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user4",
            RefreshToken = "expired-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-5),
            LastActivity = DateTime.UtcNow
        };

        var handler = CreateHandler(user);
        var token = GenerateExpiredToken(user.Id);

        var result = await handler.Handle(new RefreshTokenCommand(token, "expired-token"), default);

        Assert.False(result.Item1);
        Assert.Equal("Invalid refresh token", result.Item4);
    }
}