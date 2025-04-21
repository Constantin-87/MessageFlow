using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands;

public class RevokeRefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly RevokeRefreshTokenCommandHandler _handler;

    public RevokeRefreshTokenCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _handler = new RevokeRefreshTokenCommandHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        _userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync((ApplicationUser?)null);

        var result = await _handler.Handle(new RevokeRefreshTokenCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_UserFound_UpdatesTokenAndReturnsTrue()
    {
        var user = new ApplicationUser
        {
            Id = "user123",
            RefreshToken = "oldToken",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        _userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(new RevokeRefreshTokenCommand("user123"), CancellationToken.None);

        Assert.True(result);
        Assert.Null(user.RefreshToken);
        Assert.Equal(DateTime.MinValue, user.RefreshTokenExpiryTime);
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user123" };

        _userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed());

        var result = await _handler.Handle(new RevokeRefreshTokenCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }
}