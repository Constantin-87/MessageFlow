using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands;

public class UpdateLastActivityCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly UpdateLastActivityCommandHandler _handler;

    public UpdateLastActivityCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _handler = new UpdateLastActivityCommandHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        _userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync((ApplicationUser?)null);

        var result = await _handler.Handle(new UpdateLastActivityCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_UserFound_UpdatesLastActivity_ReturnsTrue()
    {
        var user = new ApplicationUser { Id = "user123" };

        _userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(new UpdateLastActivityCommand("user123"), CancellationToken.None);

        Assert.True(result);
        Assert.True((DateTime.UtcNow - user.LastActivity).TotalSeconds < 5); // Updated timestamp is recent
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user123" };

        _userManagerMock.Setup(x => x.FindByIdAsync("user123")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed());

        var result = await _handler.Handle(new UpdateLastActivityCommand("user123"), CancellationToken.None);

        Assert.False(result);
    }
}