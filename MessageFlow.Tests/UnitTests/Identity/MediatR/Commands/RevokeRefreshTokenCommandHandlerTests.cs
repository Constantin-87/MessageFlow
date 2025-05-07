using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Tests.Helpers.Factories;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MessageFlow.Tests.UnitTests.Identity.MediatR.Commands;

public class RevokeRefreshTokenCommandHandlerTests
{
    private RevokeRefreshTokenCommandHandler CreateHandler(ApplicationUser? user = null, bool updateSucceeds = true)
    {
        var users = user != null
            ? new[] { user }.AsQueryable()
            : Enumerable.Empty<ApplicationUser>().AsQueryable();

        var userManagerMock = UnitTestFactory.CreateMockUserManager(users);

        if (user != null)
        {
            userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
            userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(updateSucceeds ? IdentityResult.Success : IdentityResult.Failed());
        }
        else
        {
            userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
        }

        return new RevokeRefreshTokenCommandHandler(userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var handler = CreateHandler(user: null);

        var result = await handler.Handle(new RevokeRefreshTokenCommand("user123"), CancellationToken.None);

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

        var handler = CreateHandler(user);

        var result = await handler.Handle(new RevokeRefreshTokenCommand("user123"), CancellationToken.None);

        Assert.True(result);
        Assert.Null(user.RefreshToken);
        Assert.Equal(DateTime.MinValue, user.RefreshTokenExpiryTime);
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user123" };

        var handler = CreateHandler(user, updateSucceeds: false);

        var result = await handler.Handle(new RevokeRefreshTokenCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }
}