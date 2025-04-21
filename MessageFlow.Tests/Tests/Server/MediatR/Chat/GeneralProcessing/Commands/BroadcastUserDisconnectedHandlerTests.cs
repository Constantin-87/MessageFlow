using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class BroadcastUserDisconnectedHandlerTests
{
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly BroadcastUserDisconnectedHandler _handler;

    public BroadcastUserDisconnectedHandlerTests()
    {

        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _handler = new BroadcastUserDisconnectedHandler(_hubContextMock.Object);
    }

    [Fact]
    public async Task Handle_RemovesUserIfExistsInOnlineUsers()
    {
        var user = new ApplicationUserDTO
        {
            Id = "u1",
            UserName = "testuser",
            CompanyId = "comp1",
            Role = "Agent",
            LockoutEnabled = false,
            TeamsDTO = new List<TeamDTO>()
        };

        ChatHub.OnlineUsers.Clear();
        ChatHub.OnlineUsers.TryAdd("conn1", user);

        var cmd = new BroadcastUserDisconnectedCommand("comp1", "conn1");

        var result = await _handler.Handle(cmd, default);

        _clientProxyMock.Verify(p =>
            p.SendCoreAsync(
                "RemoveTeamMember",
                It.Is<object[]>(args => MatchRemoveArgs(args)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(Unit.Value, result);
    }


    [Fact]
    public async Task Handle_DoesNothingIfConnectionNotFound()
    {
        // Arrange
        ChatHub.OnlineUsers.Clear(); // no user added

        var cmd = new BroadcastUserDisconnectedCommand("missing", "comp1");

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert
        _clientProxyMock.Verify(p =>
            p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);

        Assert.Equal(Unit.Value, result);
    }

    private static bool MatchRemoveArgs(object[] args)
    {
        if (args.Length != 1) return false;
        if (args[0] is not ApplicationUserDTO dto) return false;
        return dto.Id == "u1" && dto.UserName == "testuser" && dto.CompanyId == "comp1";
    }

}
