using System.Net;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.Helpers.Interfaces;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public class SendMessageToFacebookHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILogger<SendMessageToFacebookHandler>> _loggerMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IMessageSenderHelper> _senderHelperMock = new();
        private readonly SendMessageToFacebookHandler _handler;

        public SendMessageToFacebookHandlerTests()
        {
            _handler = new SendMessageToFacebookHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object,
                _senderHelperMock.Object
            );
        }

        [Fact]
        public async Task Handle_NoSettingsFound_ReturnsFalse()
        {
            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync((FacebookSettingsModel?)null);

            var result = await _handler.Handle(new SendMessageToFacebookCommand("company1", "recip", "msg", "local1"), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_SuccessfulSend_UpdatesMessageAndReturnsTrue()
        {
            var facebookSettings = new FacebookSettingsModel
            {
                Id = "fb1",
                CompanyId = "company1",
                AccessToken = "token",
                PageId = "page1"
            };

            var message = new Message
            {
                Id = "local1",
                ConversationId = "conv1"
            };

            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(facebookSettings);

            _unitOfWorkMock.Setup(u => u.Messages.GetMessageByIdAsync("local1"))
                .ReturnsAsync(message);

            _unitOfWorkMock.Setup(u => u.Messages.UpdateEntityAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var responseContent = """{ "message_id": "fb123" }""";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _senderHelperMock
                .Setup(h => h.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), "token", It.IsAny<ILogger>()))
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _handler.Handle(
                new SendMessageToFacebookCommand("recip", "msg", "company1", "local1"), default);

            // Assert
            Assert.True(result);
            Assert.Equal("fb123", message.ProviderMessageId);
            _unitOfWorkMock.Verify(u => u.Messages.UpdateEntityAsync(It.Is<Message>(m => m.ProviderMessageId == "fb123")), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_FailureSend_LogsAndDispatchesErrorStatus()
        {
            var facebookSettings = new FacebookSettingsModel
            {
                Id = "fb1",
                CompanyId = "company1",
                AccessToken = "token",
                PageId = "page1"
            };

            var message = new Message
            {
                Id = "local1",
                ConversationId = "conv1"
            };

            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(facebookSettings);

            _unitOfWorkMock.Setup(u => u.Messages.GetMessageByIdAsync("local1"))
                .ReturnsAsync(message);

            var errorJson = """
            {
              "error": {
                "message": "Invalid recipient ID"
              }
            }
            """;

            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            };

            _senderHelperMock
                .Setup(h => h.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), "token", It.IsAny<ILogger>()))
                .ReturnsAsync(responseMessage);

            ProcessMessageStatusUpdateCommand? capturedCommand = null;

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), It.IsAny<CancellationToken>()))
                .Callback<object, CancellationToken>((req, _) =>
                {
                    capturedCommand = req as ProcessMessageStatusUpdateCommand;
                })
                .ReturnsAsync(true);

            var result = await _handler.Handle(
                new SendMessageToFacebookCommand("recip", "msg", "company1", "local1"), default);

            Assert.False(result);
            Assert.NotNull(capturedCommand);
            Assert.Equal("Facebook", capturedCommand!.Platform);
            Assert.True(capturedCommand.StatusElement.TryGetProperty("status", out var statusProp));
            Assert.Equal("error", statusProp.GetString());
        }
    }
}
