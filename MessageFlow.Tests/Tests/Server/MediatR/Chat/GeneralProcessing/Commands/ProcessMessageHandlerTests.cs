using MediatR;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands;

public class ProcessMessageHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ProcessMessageHandler _handler;

    public ProcessMessageHandlerTests()
    {
        _handler = new ProcessMessageHandler(
            _unitOfWorkMock.Object,
            _mediatorMock.Object
        );
    }

    [Fact]
    public async Task Handle_ExistingActiveAIConversation_DelegatesToAIHandler()
    {
        var convo = new Conversation { Id = "c1", CompanyId = "comp1", SenderId = "s1", IsActive = true, AssignedUserId = "AI" };
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationBySenderAndCompanyAsync("s1", "comp1"))
            .ReturnsAsync(convo);

        var cmd = new ProcessMessageCommand("comp1", "s1", "User", "Hi", "prov1", "Facebook");

        await _handler.Handle(cmd, default);

        _mediatorMock.Verify(m => m.Send(It.Is<HandleAIConversationCommand>(x =>
            x.Conversation == convo && x.MessageText == "Hi" && x.ProviderMessageId == "prov1"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_ExistingActiveAssignedUser_DelegatesToAddMessage()
    {
        var convo = new Conversation { Id = "c2", CompanyId = "comp1", SenderId = "s1", IsActive = true, AssignedUserId = "u1" };
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationBySenderAndCompanyAsync("s1", "comp1"))
            .ReturnsAsync(convo);

        var cmd = new ProcessMessageCommand("comp1", "s1", "User", "Hey there", "prov2", "Facebook");

        await _handler.Handle(cmd, default);

        _mediatorMock.Verify(m => m.Send(It.Is<AddMessageToConversationCommand>(x =>
            x.Conversation == convo && x.MessageText == "Hey there" && x.ProviderMessageId == "prov2"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_NoActiveConversation_CreatesAndAssignsToAI()
    {
        _unitOfWorkMock.Setup(u => u.Conversations.GetConversationBySenderAndCompanyAsync("s2", "comp1"))
            .ReturnsAsync((Conversation?)null);

        var cmd = new ProcessMessageCommand("comp1", "s2", "John", "Need help", "prov3", "WhatsApp");

        await _handler.Handle(cmd, default);

        _mediatorMock.Verify(m => m.Send(It.Is<CreateAndAssignToAICommand>(x =>
            x.CompanyId == "comp1" && x.SenderId == "s2" && x.Username == "John" &&
            x.MessageText == "Need help" && x.ProviderMessageId == "prov3" && x.Source == "WhatsApp"), It.IsAny<CancellationToken>()));
    }
}