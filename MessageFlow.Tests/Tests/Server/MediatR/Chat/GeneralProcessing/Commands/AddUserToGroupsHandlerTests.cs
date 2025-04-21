using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class AddUserToGroupsHandlerTests
{
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
    private readonly Mock<IGroupManager> _groupManagerMock = new();
    private readonly AddUserToGroupsHandler _handler;

    public AddUserToGroupsHandlerTests()
    {
        _hubContextMock.Setup(x => x.Groups).Returns(_groupManagerMock.Object);
        _handler = new AddUserToGroupsHandler(_hubContextMock.Object);
    }

    [Fact]
    public async Task Handle_AddsUserToCompanyAndTeamGroups()
    {
        // Arrange
        var user = new ApplicationUserDTO
        {
            Id = "user1",
            CompanyId = "comp1",
            TeamsDTO = new List<TeamDTO>
            {
                new() { Id = "team1" },
                new() { Id = "team2" }
            }
        };

        var command = new AddUserToGroupsCommand(user, "conn1");

        _groupManagerMock.Setup(g => g.AddToGroupAsync("conn1", "Company_comp1", It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        _groupManagerMock.Setup(g => g.AddToGroupAsync("conn1", "Team_team1", It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        _groupManagerMock.Setup(g => g.AddToGroupAsync("conn1", "Team_team2", It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync("conn1", "Company_comp1", It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(g => g.AddToGroupAsync("conn1", "Team_team1", It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(g => g.AddToGroupAsync("conn1", "Team_team2", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_AddsUserToCompanyGroup_WithoutTeams()
    {
        // Arrange
        var user = new ApplicationUserDTO
        {
            Id = "user2",
            CompanyId = "comp2",
            TeamsDTO = null
        };

        var command = new AddUserToGroupsCommand(user, "conn2");

        _groupManagerMock.Setup(g => g.AddToGroupAsync("conn2", "Company_comp2", It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync("conn2", "Company_comp2", It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Team_")), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(Unit.Value, result);
    }
}
