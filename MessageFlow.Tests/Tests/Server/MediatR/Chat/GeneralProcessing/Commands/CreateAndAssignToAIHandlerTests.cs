using Moq;
using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class CreateAndAssignToAIHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly CreateAndAssignToAIHandler _handler;

    public CreateAndAssignToAIHandlerTests()
    {
        _handler = new CreateAndAssignToAIHandler(
            _unitOfWorkMock.Object,
            _mediatorMock.Object
        );
    }

    [Fact]
    public async Task Handle_CreatesConversationAndSendsToAIHandler()
    {
        // Arrange
        var cmd = new CreateAndAssignToAICommand(
            CompanyId: "comp1",
            SenderId: "sender1",
            Username: "user1",
            MessageText: "Hello AI",
            ProviderMessageId: "prov1",
            Source: "Widget"
        );

        _unitOfWorkMock.Setup(u => u.Conversations.AddEntityAsync(It.IsAny<Conversation>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _mediatorMock.Setup(m => m.Send(It.IsAny<HandleAIConversationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert
        _unitOfWorkMock.Verify(u => u.Conversations.AddEntityAsync(It.Is<Conversation>(c =>
            c.SenderId == "sender1" &&
            c.SenderUsername == "user1" &&
            c.CompanyId == "comp1" &&
            c.AssignedUserId == "AI" &&
            c.Source == "Widget" &&
            c.IsActive
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<HandleAIConversationCommand>(x =>
            x.MessageText == "Hello AI" &&
            x.ProviderMessageId == "prov1" &&
            x.Conversation.SenderId == "sender1"
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(Unit.Value, result);
    }
}