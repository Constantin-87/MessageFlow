using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public class AddMessageToConversationHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _clientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly Mock<ILogger<AddMessageToConversationHandler>> _loggerMock = new();
        private readonly AddMessageToConversationHandler _handler;

        public AddMessageToConversationHandlerTests()
        {
            _clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

            _handler = new AddMessageToConversationHandler(
                _unitOfWorkMock.Object,
                _hubContextMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsMessageAndNotifiesAssignedUser()
        {
            var conversation = new Conversation
            {
                Id = "conv1",
                AssignedUserId = "user1"
            };

            var command = new AddMessageToConversationCommand(
                conversation,
                "user1",
                "Hello",
                "prov-id"
            );


            _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, default);

            _unitOfWorkMock.Verify(u => u.Messages.AddEntityAsync(It.Is<Message>(m =>
                m.ConversationId == "conv1" && m.Content == "Hello" && m.ProviderMessageId == "prov-id"
            )), Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

            _clientProxyMock.Verify(c =>
            c.SendCoreAsync(
                "SendMessageToAssignedUser",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0] == conversation &&
                    args[1] is Message &&
                    ((Message)args[1]).Content == "Hello"),
                default),
            Times.Once);


            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_UnassignedConversation_AddsMessageWithoutNotifying()
        {
            var conversation = new Conversation
            {
                Id = "conv1",
                AssignedUserId = null
            };

            var command = new AddMessageToConversationCommand(
                conversation,
                "user1",
                "Hello",
                "prov-id"
            );

            _unitOfWorkMock.Setup(u => u.Messages.AddEntityAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                            .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, default);

            _clientProxyMock.Verify(p =>
            p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                default),
            Times.Never);

            Assert.Equal(Unit.Value, result);
        }
    }
}
