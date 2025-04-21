using System.Security.Claims;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands;

public class LogoutCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _handler = new LogoutCommandHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_NoUserId_ReturnsFalse()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // no NameIdentifier
        var command = new LogoutCommand(claimsPrincipal);

        var result = await _handler.Handle(command, default);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var userId = "123";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        var result = await _handler.Handle(new LogoutCommand(claimsPrincipal), default);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_ValidUser_UpdatesRefreshToken_ReturnsTrue()
    {
        var userId = "456";
        var user = new ApplicationUser
        {
            Id = userId,
            RefreshToken = "token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(new LogoutCommand(claimsPrincipal), default);

        Assert.True(result);
        Assert.Null(user.RefreshToken);
        Assert.Equal(DateTime.MinValue, user.RefreshTokenExpiryTime);
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFalse()
    {
        var userId = "789";
        var user = new ApplicationUser { Id = userId };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed());

        var result = await _handler.Handle(new LogoutCommand(claimsPrincipal), default);

        Assert.False(result);
    }
}