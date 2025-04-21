using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Handlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class BroadcastTeamMembersHandlerTests
{
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly BroadcastTeamMembersHandler _handler;

    public BroadcastTeamMembersHandlerTests()
    {
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _handler = new BroadcastTeamMembersHandler(_hubContextMock.Object);
    }

    [Fact]
    public async Task Handle_SendsEachOnlineUserToCompanyGroup()
    {
        // Arrange
        var user1 = new ApplicationUserDTO { Id = "u1", CompanyId = "comp1" };
        var user2 = new ApplicationUserDTO { Id = "u2", CompanyId = "comp1" };
        var user3 = new ApplicationUserDTO { Id = "u3", CompanyId = "comp2" }; // different company

        ChatHub.OnlineUsers.Clear();
        ChatHub.OnlineUsers.TryAdd("conn1", user1);
        ChatHub.OnlineUsers.TryAdd("conn2", user2);
        ChatHub.OnlineUsers.TryAdd("conn3", user3);

        var cmd = new BroadcastTeamMembersCommand("comp1");

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert
        _clientProxyMock.Verify(p =>
            p.SendCoreAsync(
                "AddTeamMember",
                It.Is<object[]>(args => args.Length == 1 && args[0] == user1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _clientProxyMock.Verify(p =>
            p.SendCoreAsync(
                "AddTeamMember",
                It.Is<object[]>(args => args.Length == 1 && args[0] == user2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _clientProxyMock.Verify(p =>
            p.SendCoreAsync(
                "AddTeamMember",
                It.Is<object[]>(args => args.Length == 1 && args[0] == user3),
                It.IsAny<CancellationToken>()),
            Times.Never);

        Assert.Equal(Unit.Value, result);
    }
}
