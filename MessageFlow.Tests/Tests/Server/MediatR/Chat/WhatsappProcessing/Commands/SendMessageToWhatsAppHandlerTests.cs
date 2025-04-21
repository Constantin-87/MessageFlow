using System.Net;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Helpers.Interfaces;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public class SendMessageToWhatsAppHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
        private readonly Mock<IMessageSenderHelper> _senderMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<ILogger<SendMessageToWhatsAppHandler>> _loggerMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();

        private readonly SendMessageToWhatsAppHandler _handler;

        public SendMessageToWhatsAppHandlerTests()
        {
            _hubClientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);

            _handler = new SendMessageToWhatsAppHandler(
                _unitOfWorkMock.Object,
                _hubContextMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object,
                _senderMock.Object);
        }

        [Fact]
        public async Task Handle_SuccessfulSend_UpdatesMessageAndNotifiesClient()
        {
            var message = new Message
            {
                Id = "local1",
                ConversationId = "conv1",
                UserId = "user1",
                Username = "Test User",
                Content = "Test content",
                Status = "pending"
            };

            var waSettings = new WhatsAppSettingsModel
            {
                CompanyId = "company1",
                AccessToken = "token",
                PhoneNumbers = new List<PhoneNumberInfo>
                {
                    new() { PhoneNumberId = "pn1" }
                }
            };

            var json = """{ "messages": [ { "id": "msg123" } ] }""";

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(waSettings);

            _senderMock.Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), "token", It.IsAny<ILogger>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            _unitOfWorkMock.Setup(u => u.Messages.GetMessageByIdAsync("local1")).ReturnsAsync(message);
            _unitOfWorkMock.Setup(u => u.Messages.UpdateEntityAsync(message)).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var cmd = new SendMessageToWhatsAppCommand("+111", "hi", "company1", "local1");

            var result = await _handler.Handle(cmd, default);

            Assert.Equal("msg123", message.ProviderMessageId);
            _clientProxyMock.Verify(c => c.SendCoreAsync("MessageStatusUpdated", It.Is<object[]>(o => o[1]!.ToString() == "Sent"), default), Times.Once);
        }
        [Fact]
        public async Task Handle_FailedSend_LogsAndSendsStatusUpdate()
        {
            var waSettings = new WhatsAppSettingsModel
            {
                CompanyId = "company1",
                AccessToken = "token",
                PhoneNumbers = new List<PhoneNumberInfo>
                {
                    new() { PhoneNumberId = "pn1" }
                }
            };

            var errorJson = """{ "error": { "message": "Invalid phone" } }""";

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(waSettings);

            _senderMock.Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), "token", It.IsAny<ILogger>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
                });

            _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);


            var cmd = new SendMessageToWhatsAppCommand("+111", "hi", "company1", "local1");

            var result = await _handler.Handle(cmd, default);

            _mediatorMock.Verify(m => m.Send(It.Is<ProcessMessageStatusUpdateCommand>(c =>
                c.Platform == "WhatsApp"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NoWhatsAppSettings_ReturnsEarly()
        {
            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync((WhatsAppSettingsModel?)null);

            var cmd = new SendMessageToWhatsAppCommand("+111", "hi", "company1", "local1");

            var result = await _handler.Handle(cmd, default);

            _senderMock.Verify(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never);
            _mediatorMock.Verify(m => m.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), default), Times.Never);
        }

        [Fact]
        public async Task Handle_SettingsExistButNoPhoneNumbers_ReturnsEarly()
        {
            var settings = new WhatsAppSettingsModel
            {
                CompanyId = "company1",
                PhoneNumbers = new List<PhoneNumberInfo>()
            };

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(settings);

            var cmd = new SendMessageToWhatsAppCommand("+111", "hi", "company1", "local1");

            var result = await _handler.Handle(cmd, default);

            _senderMock.Verify(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SuccessfulSend_ButMessageNotFound_DoesNotCrash()
        {
            var settings = new WhatsAppSettingsModel
            {
                CompanyId = "company1",
                AccessToken = "token",
                PhoneNumbers = new List<PhoneNumberInfo> { new() { PhoneNumberId = "pn1" } }
            };

            var json = """{ "messages": [ { "id": "msg123" } ] }""";

            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByCompanyIdAsync("company1"))
                .ReturnsAsync(settings);

            _senderMock.Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>(), "token", It.IsAny<ILogger>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            _unitOfWorkMock.Setup(u => u.Messages.GetMessageByIdAsync("local1"))
                .ReturnsAsync((Message?)null);

            var cmd = new SendMessageToWhatsAppCommand("+111", "hi", "company1", "local1");

            var result = await _handler.Handle(cmd, default);

            _unitOfWorkMock.Verify(u => u.Messages.UpdateEntityAsync(It.IsAny<Message>()), Times.Never);
            _clientProxyMock.Verify(c => c.SendCoreAsync("MessageStatusUpdated", It.IsAny<object[]>(), default), Times.Once);
        }
    }
}