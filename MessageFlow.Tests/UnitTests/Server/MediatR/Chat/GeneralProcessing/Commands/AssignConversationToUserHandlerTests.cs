using Moq;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class AssignConversationToUserHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IHubClients> _hubClientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly AssignConversationToUserHandler _handler;

    public AssignConversationToUserHandlerTests()
    {
        _hubClientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);

        _handler = new AssignConversationToUserHandler(
            _unitOfWorkMock.Object,
            _hubContextMock.Object,
            _mapperMock.Object
        );
    }

    [Fact]
    public async Task Handle_ConversationFound_AssignsAndNotifies()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            CompanyId = "comp1",
            Messages = new List<Message>
        {
            new()
            {
                Id = "m1",
                Content = "Hello",
                ConversationId = "conv1",
                SentAt = DateTime.UtcNow
            }
        }
        };

        var dto = new ConversationDTO
        {
            Id = "conv1",
            CompanyId = "comp1",
            Messages = new List<MessageDTO>
        {
            new()
            {
                Id = "m1",
                Content = "Hello",
                ConversationId = "conv1",
                SentAt = DateTime.UtcNow
            }
        }
        };

        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationByIdAsync("conv1")).ReturnsAsync(conversation);
        _mapperMock.Setup(m => m.Map<ConversationDTO>(conversation)).Returns(dto);
        _unitOfWorkMock.Setup(u => u.Conversations.UpdateEntityAsync(conversation)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        var cmd = new AssignConversationToUserCommand("conv1", "user123");

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert
        Assert.True(result.Success);
        _unitOfWorkMock.Verify(u => u.Conversations.UpdateEntityAsync(It.Is<Conversation>(c => c.AssignedUserId == "user123" && c.IsAssigned)), Times.Once);
        _mapperMock.Verify(m => m.Map<ConversationDTO>(It.IsAny<Conversation>()), Times.Once);

        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "AssignConversation",
            It.Is<object[]>(args => args[0] == dto),
            It.IsAny<CancellationToken>()),
        Times.Once);

        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "RemoveNewConversation",
            It.Is<object[]>(args => args[0] == dto),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsFalse()
    {
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationByIdAsync("missing")).ReturnsAsync((Conversation?)null);

        var cmd = new AssignConversationToUserCommand("missing", "user123");
        var result = await _handler.Handle(cmd, default);

        Assert.False(result.Success);
        Assert.Equal("Conversation not found.", result.ErrorMessage);
    }
}
