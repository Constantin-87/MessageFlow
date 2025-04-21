using System.Text.Json;
using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public class HandleFacebookReadEventHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<HandleFacebookReadEventHandler>> _loggerMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly HandleFacebookReadEventHandler _handler;

        public HandleFacebookReadEventHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<HandleFacebookReadEventHandler>>();
            _mediatorMock = new Mock<IMediator>();
            _handler = new HandleFacebookReadEventHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object);
        }

        [Fact]
        public async Task Handle_MissingWatermark_LogsAndReturns()
        {
            var json = JsonDocument.Parse("{}").RootElement;

            var result = await _handler.Handle(new HandleFacebookReadEventCommand(json, "sender", "page"), default);

            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_InvalidWatermark_LogsAndReturns()
        {
            var json = JsonDocument.Parse("{\"watermark\": \"invalid\"}").RootElement;

            var result = await _handler.Handle(new HandleFacebookReadEventCommand(json, "sender", "page"), default);

            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_NoSettingsFound_LogsAndReturns()
        {
            var watermark = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var json = JsonDocument.Parse($"{{\"watermark\": {watermark}}}").RootElement;

            _unitOfWorkMock.Setup(x => x.FacebookSettings.GetSettingsByPageIdAsync("page"))
                .ReturnsAsync((FacebookSettingsModel?)null);

            var result = await _handler.Handle(new HandleFacebookReadEventCommand(json, "sender", "page"), default);

            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_NoConversationFound_LogsAndReturns()
        {
            var watermark = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var json = JsonDocument.Parse($"{{\"watermark\": {watermark}}}").RootElement;

            _unitOfWorkMock.Setup(x => x.FacebookSettings.GetSettingsByPageIdAsync("page"))
                .ReturnsAsync(new FacebookSettingsModel
                {
                    Id = "fb1",
                    PageId = "page",
                    AccessToken = "token123",
                    CompanyId = "company"
                });

            _unitOfWorkMock.Setup(x => x.Conversations.GetConversationBySenderAndCompanyAsync("sender", "company"))
                .ReturnsAsync((Conversation?)null);

            var result = await _handler.Handle(new HandleFacebookReadEventCommand(json, "sender", "page"), default);

            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ProcessesUnreadMessagesAndSaves()
        {
            var watermark = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var json = JsonDocument.Parse($"{{\"watermark\": {watermark}}}").RootElement;

            var conversation = new Conversation { Id = "conv1", CompanyId = "company" };
            var messages = new List<Message>
            {
                new() { Id = "msg1", ProviderMessageId = "pmsg1", ConversationId = "conv1" },
                new() { Id = "msg2", ProviderMessageId = "pmsg2", ConversationId = "conv1" }
            };

            _unitOfWorkMock.Setup(x => x.FacebookSettings.GetSettingsByPageIdAsync("page"))
                .ReturnsAsync(new FacebookSettingsModel
                {
                    Id = "fb1",
                    PageId = "page",
                    AccessToken = "token123",
                    CompanyId = "company"
                });

            _unitOfWorkMock.Setup(x => x.Conversations.GetConversationBySenderAndCompanyAsync("sender", "company"))
                .ReturnsAsync(conversation);

            _unitOfWorkMock.Setup(x => x.Messages.GetUnreadMessagesBeforeTimestampAsync("conv1", It.IsAny<DateTime>()))
                .ReturnsAsync(messages);

            _mediatorMock.Setup(x => x.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), default))
                .ReturnsAsync(true);

            var result = await _handler.Handle(new HandleFacebookReadEventCommand(json, "sender", "page"), default);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _mediatorMock.Verify(x => x.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), default), Times.Exactly(2));
            Assert.Equal(Unit.Value, result);
        }
    }
}
