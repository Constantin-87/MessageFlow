using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Tests.Helpers.Factories;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MessageFlow.Tests.UnitTests.Identity.MediatR.Commands;

public class UpdateLastActivityCommandHandlerTests
{
    private UpdateLastActivityCommandHandler CreateHandler(ApplicationUser? user = null, bool updateSucceeds = true)
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

        return new UpdateLastActivityCommandHandler(userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateLastActivityCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_UserFound_UpdatesLastActivity_ReturnsTrue()
    {
        var user = new ApplicationUser { Id = "user123" };

        var handler = CreateHandler(user);

        var result = await handler.Handle(new UpdateLastActivityCommand("user123"), CancellationToken.None);

        Assert.True(result);
        Assert.True((DateTime.UtcNow - user.LastActivity).TotalSeconds < 5);
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user123" };

        var handler = CreateHandler(user, updateSucceeds: false);

        var result = await handler.Handle(new UpdateLastActivityCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }
}