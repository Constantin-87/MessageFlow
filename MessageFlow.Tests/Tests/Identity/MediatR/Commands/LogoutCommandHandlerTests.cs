using System.Security.Claims;
using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands;

public class LogoutCommandHandlerTests
{
    private LogoutCommandHandler CreateHandlerWithUser(
        string? userId = null,
        ApplicationUser? user = null,
        bool updateSuccess = true)
    {
        var users = user != null
            ? new[] { user }.AsQueryable()
            : Enumerable.Empty<ApplicationUser>().AsQueryable();

        var userManagerMock = TestDbContextFactory.CreateMockUserManager(users);

        if (userId != null)
        {
            userManagerMock
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            userManagerMock
                .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(updateSuccess ? IdentityResult.Success : IdentityResult.Failed());
        }

        return new LogoutCommandHandler(userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_NoUserId_ReturnsFalse()
    {
        var handler = CreateHandlerWithUser();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // no NameIdentifier

        var result = await handler.Handle(new LogoutCommand(claimsPrincipal), default);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var userId = "123";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var handler = CreateHandlerWithUser(userId: userId, user: null);

        var result = await handler.Handle(new LogoutCommand(claimsPrincipal), default);

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

        var handler = CreateHandlerWithUser(userId, user);

        var result = await handler.Handle(new LogoutCommand(claimsPrincipal), default);

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

        var handler = CreateHandlerWithUser(userId, user, updateSuccess: false);

        var result = await handler.Handle(new LogoutCommand(claimsPrincipal), default);

        Assert.False(result);
    }
}