using MediatR;
using Moq;
using Xunit;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Commands;
using MessageFlow.Server.DataTransferObjects.Internal;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class HandleAIConversationHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly HandleAIConversationHandler _handler;

    public HandleAIConversationHandlerTests()
    {
        _handler = new HandleAIConversationHandler(
            _unitOfWorkMock.Object,
            _mediatorMock.Object
        );
    }

    [Fact]
    public async Task Handle_Escalates_WhenTeamIdProvided()
    {
        var convo = new Conversation { Id = "c1", CompanyId = "comp1", SenderId = "s1" };

        _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        _mediatorMock.Setup(m => m.Send(It.IsAny<HandleUserQueryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQueryResponseDTO
            {
                Answered = true,
                TargetTeamId = "team1",
                TargetTeamName = "Sales",
                RawResponse = "irrelevant"
            });

        _mediatorMock.Setup(m => m.Send(It.IsAny<EscalateCompanyTeamCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _handler.Handle(new HandleAIConversationCommand(convo, "Hello", "prov1"), default);

        _mediatorMock.Verify(m => m.Send(It.Is<EscalateCompanyTeamCommand>(x =>
            x.Conversation == convo &&
            x.TargetTeamId == "team1"), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_SendsAIResponse_WhenNoTeamId()
    {
        var convo = new Conversation { Id = "c2", CompanyId = "comp2", SenderId = "s2" };

        _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        _mediatorMock.Setup(m => m.Send(It.IsAny<HandleUserQueryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQueryResponseDTO
            {
                Answered = true,
                TargetTeamId = null,
                RawResponse = "AI response"
            });

        _mediatorMock.Setup(m => m.Send(It.IsAny<SendAIResponseCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _handler.Handle(new HandleAIConversationCommand(convo, "Help me", "prov2"), default);

        _mediatorMock.Verify(m => m.Send(It.Is<SendAIResponseCommand>(x =>
            x.Conversation == convo &&
            x.Response == "AI response"), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(Unit.Value, result);
    }
}
