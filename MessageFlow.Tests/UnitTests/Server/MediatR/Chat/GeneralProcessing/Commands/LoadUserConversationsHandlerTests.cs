using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.DataAccess.Repositories;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class LoadUserConversationsHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly LoadUserConversationsHandler _handler;

    public LoadUserConversationsHandlerTests()
    {
        _handler = new LoadUserConversationsHandler(
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    [Fact]
    public async Task Handle_LoadsAssignedAndUnassignedConversations_AndSendsToCaller()
    {
        // Arrange
        var userId = "user1";
        var companyId = "comp1";
        var assigned = new List<Conversation> { new() { Id = "a1" } };
        var unassigned = new List<Conversation> { new() { Id = "u1", AssignedTeamId = "team1" } };
        var assignedDto = new List<ConversationDTO> { new() { Id = "a1" } };
        var unassignedDto = new List<ConversationDTO> { new() { Id = "u1" } };
        // Mock user repo
        var appUserRepoMock = new Mock<IApplicationUserRepository>();
        appUserRepoMock.Setup(r => r.GetUserByIdAsync(userId))
            .ReturnsAsync(new ApplicationUser
            {
                Id = userId,
                Teams = new List<Team> { new() { Id = "team1" } }
            });
        _unitOfWorkMock.Setup(u => u.ApplicationUsers).Returns(appUserRepoMock.Object);

        _unitOfWorkMock.Setup(u => u.Conversations.GetAssignedConversationsAsync(userId, companyId))
            .ReturnsAsync(assigned);
        _unitOfWorkMock.Setup(u => u.Conversations.GetUnassignedConversationsAsync(companyId))
            .ReturnsAsync(unassigned);
        _mapperMock.Setup(m => m.Map<List<ConversationDTO>>(assigned)).Returns(assignedDto);
        _mapperMock.Setup(m => m.Map<List<ConversationDTO>>(unassigned)).Returns(unassignedDto);

        var command = new LoadUserConversationsCommand(userId, companyId, _clientProxyMock.Object);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "LoadAssignedConversations",
            It.Is<object[]>(args => args.Length == 1 && args[0] == assignedDto),
            It.IsAny<CancellationToken>()), Times.Once);

        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "LoadNewConversations",
            It.Is<object[]>(args => args.Length == 1 && args[0] == unassignedDto),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(Unit.Value, result);
    }
}
