using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public class EscalateCompanyTeamHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _clientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly Mock<ILogger<EscalateCompanyTeamHandler>> _loggerMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();

        private readonly EscalateCompanyTeamHandler _handler;

        public EscalateCompanyTeamHandlerTests()
        {
            _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

            _handler = new EscalateCompanyTeamHandler(
                _unitOfWorkMock.Object,
                _hubContextMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );
        }

        [Theory]
        [InlineData("Facebook")]
        [InlineData("WhatsApp")]
        [InlineData("Unknown")]
        public async Task Handle_Escalation_WorksForAllSources(string source)
        {
            var conversation = new Conversation
            {
                Id = "conv1",
                Source = source,
                CompanyId = "comp1"
            };

            var cmd = new EscalateCompanyTeamCommand(
                conversation,
                SenderId: "user1",
                ProviderMessageId: "prov123",
                TargetTeamId: "team2",
                TargetTeamName: "Sales"
            );

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);

            _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _handler.Handle(cmd, default);

            _clientProxyMock.Verify(p =>
                p.SendCoreAsync(
                    "NewConversationAdded",
                    It.Is<object[]>(args => args.Length == 1 && args[0] == conversation),
                    default),
                Times.Once);

            _clientProxyMock.Verify(p =>
                p.SendCoreAsync(
                    "SendMessageToAssignedUser",
                    It.Is<object[]>(args => args.Length == 2 &&
                        args[0] == conversation &&
                        args[1] is Message),
                    default),
                Times.Once);

            var expectedText = $"Your request is being redirected to the {cmd.TargetTeamName} team. Please wait for an available agent.";

            if (source == "Facebook")
            {
                _mediatorMock.Verify(m => m.Send(It.Is<SendMessageToFacebookCommand>(x =>
                    x.MessageText == expectedText &&
                    x.CompanyId == cmd.Conversation.CompanyId &&
                    x.RecipientId == cmd.SenderId &&
                    x.LocalMessageId == cmd.ProviderMessageId
                ), default), Times.Once);
            }
            else if (source == "WhatsApp")
            {
                _mediatorMock.Verify(m => m.Send(It.Is<SendMessageToWhatsAppCommand>(x =>
                    x.MessageText == expectedText &&
                    x.CompanyId == cmd.Conversation.CompanyId &&
                    x.RecipientPhoneNumber == cmd.SenderId &&
                    x.LocalMessageId == cmd.ProviderMessageId
                ), default), Times.Once);
            }

            _unitOfWorkMock.Verify(u => u.Messages.AddEntityAsync(It.Is<Message>(m =>
                m.UserId == "AI" && m.Content.Contains("redirected to the Sales team"))), Times.Once);

            Assert.Equal(Unit.Value, result);
        }
    }
}
