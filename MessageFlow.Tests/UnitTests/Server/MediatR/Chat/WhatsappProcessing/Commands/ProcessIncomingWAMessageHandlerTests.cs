using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public class ProcessIncomingWAMessageHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<ILogger<ProcessIncomingWAMessageHandler>> _loggerMock = new();
        private readonly ProcessIncomingWAMessageHandler _handler;

        public ProcessIncomingWAMessageHandlerTests()
        {
            _handler = new ProcessIncomingWAMessageHandler(
                _unitOfWorkMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_NoSettings_LogsAndReturns()
        {
            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByBusinessAccountIdAsync("biz123"))
                .ReturnsAsync((WhatsAppSettingsModel?)null);

            var json = JsonDocument.Parse("""[{ "value": {} }]""").RootElement;

            var result = await _handler.Handle(new ProcessIncomingWAMessageCommand("biz123", json), default);

            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ProcessesStatusAndMessages()
        {
            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByBusinessAccountIdAsync("biz123"))
                .ReturnsAsync(new WhatsAppSettingsModel { CompanyId = "company1" });

            var json = JsonDocument.Parse("""
            [{
                "value": {
                    "statuses": [
                        { "id": "s1", "timestamp": "1" },
                        { "id": "s2", "timestamp": "2" }
                    ],
                    "contacts": [
                        {
                            "wa_id": "user1",
                            "profile": { "name": "User One" }
                        }
                    ],
                    "messages": [
                        {
                            "id": "m1",
                            "text": { "body": "Hi" }
                        }
                    ]
                }
            }]
            """).RootElement;

            var result = await _handler.Handle(new ProcessIncomingWAMessageCommand("biz123", json), default);

            _mediatorMock.Verify(m => m.Send(It.Is<ProcessMessageStatusUpdateCommand>(c =>
                c.Platform == "WhatsApp"), default), Times.Exactly(2));

            _mediatorMock.Verify(m => m.Send(It.Is<ProcessMessageCommand>(c =>
                c.Source == "WhatsApp" &&
                c.CompanyId == "company1" &&
                c.SenderId == "user1" &&
                c.Username == "User One" &&
                c.MessageText == "Hi" &&
                c.ProviderMessageId == "m1"
            ), default), Times.Once);


            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ChangeEntryWithoutValue_LogsWarningAndSkips()
        {
            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByBusinessAccountIdAsync("biz123"))
                .ReturnsAsync(new WhatsAppSettingsModel { CompanyId = "company1" });

            var json = JsonDocument.Parse("""[{ "no_value": true }]""").RootElement;

            var result = await _handler.Handle(new ProcessIncomingWAMessageCommand("biz123", json), default);

            _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<Unit>>(), default), Times.Never);
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ContactsWithoutMessages_DoesNotCrash()
        {
            _unitOfWorkMock.Setup(u => u.WhatsAppSettings.GetSettingsByBusinessAccountIdAsync("biz123"))
                .ReturnsAsync(new WhatsAppSettingsModel { CompanyId = "company1" });

            var json = JsonDocument.Parse("""[{"value": {"contacts": [{"wa_id": "user1","profile": { "name": "User One" }}]}}]""").RootElement;

            var result = await _handler.Handle(new ProcessIncomingWAMessageCommand("biz123", json), default);

            _mediatorMock.Verify(m => m.Send(It.IsAny<ProcessMessageCommand>(), default), Times.Never);
            Assert.Equal(Unit.Value, result);
        }
    }
}