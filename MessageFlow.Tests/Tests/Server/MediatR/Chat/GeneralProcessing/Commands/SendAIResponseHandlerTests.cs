using Moq;
using MediatR;
using Microsoft.Extensions.Logging;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class SendAIResponseHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<SendAIResponseHandler>> _loggerMock = new();
    private readonly SendAIResponseHandler _handler;

    public SendAIResponseHandlerTests()
    {
        _handler = new SendAIResponseHandler(_unitOfWorkMock.Object, _loggerMock.Object, _mediatorMock.Object);
    }

    [Theory]
    [InlineData("Facebook")]
    [InlineData("WhatsApp")]
    [InlineData("Unknown")]
    public async Task Handle_SendsMessageAndDispatchesPlatformCommand(string source)
    {
        var convo = new Conversation { Id = "conv1", SenderId = "user1", CompanyId = "comp1", Source = source };

        _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        var cmd = new SendAIResponseCommand(convo, "Here's your AI answer.", "prov123");
        var result = await _handler.Handle(cmd, default);

        _unitOfWorkMock.Verify(u => u.Messages.AddEntityAsync(It.Is<Message>(m =>
            m.ConversationId == "conv1" &&
            m.UserId == "AI" &&
            m.Content == "Here's your AI answer.")), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

        if (source == "Facebook")
        {
            _mediatorMock.Verify(m => m.Send(It.Is<SendMessageToFacebookCommand>(x =>
                x.RecipientId == "user1" &&
                x.MessageText == "Here's your AI answer." &&
                x.CompanyId == "comp1" &&
                x.LocalMessageId == "prov123"
            ), default), Times.Once);
        }
        else if (source == "WhatsApp")
        {
            _mediatorMock.Verify(m => m.Send(It.Is<SendMessageToWhatsAppCommand>(x =>
                x.RecipientPhoneNumber == "user1" &&
                x.MessageText == "Here's your AI answer." &&
                x.CompanyId == "comp1" &&
                x.LocalMessageId == "prov123"
            ), default), Times.Once);
        }

        Assert.Equal(Unit.Value, result);
    }
}
