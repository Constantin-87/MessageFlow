using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class ProcessMessageStatusUpdateHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<ProcessMessageStatusUpdateHandler>> _loggerMock = new();

    private readonly ProcessMessageStatusUpdateHandler _handler;

    public ProcessMessageStatusUpdateHandlerTests()
    {
        _clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _handler = new ProcessMessageStatusUpdateHandler(
            _unitOfWorkMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_MessageFound_StatusUpdated_NotifiesUser()
    {
        var statusJson = JsonDocument.Parse("""
        {
            "id": "prov1",
            "status": "delivered",
            "timestamp": "1700000000"
        }
        """).RootElement;

        var message = new Message
        {
            Id = "msg1",
            ProviderMessageId = "prov1",
            ConversationId = "conv1",
            Status = "sent"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            AssignedUserId = "user1"
        };

        ChatHub.OnlineUsers.Clear();
        ChatHub.OnlineUsers.TryAdd("conn1", new Shared.DTOs.ApplicationUserDTO { Id = "user1" });

        _unitOfWorkMock.Setup(u => u.Messages.GetMessageByProviderIdAsync("prov1"))
            .ReturnsAsync(message);
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationByIdAsync("conv1"))
            .ReturnsAsync(conversation);
        _unitOfWorkMock.Setup(u => u.Messages.UpdateEntityAsync(message))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var cmd = new ProcessMessageStatusUpdateCommand(statusJson, "WhatsApp");
        var result = await _handler.Handle(cmd, default);

        Assert.True(result);
        Assert.Equal("delivered", message.Status);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "MessageStatusUpdated",
            It.Is<object[]>(a => a[0]!.ToString() == "msg1" && a[1]!.ToString() == "delivered"),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_MessageNotFound_ReturnsFalse()
    {
        var json = JsonDocument.Parse("""{ "id": "unknown", "status": "sent", "timestamp": "1700000000" }""").RootElement;
        _unitOfWorkMock.Setup(u => u.Messages.GetMessageByProviderIdAsync("unknown"))
            .ReturnsAsync((Message?)null);

        var result = await _handler.Handle(new ProcessMessageStatusUpdateCommand(json, "Facebook"), default);
        Assert.False(result);
    }
}