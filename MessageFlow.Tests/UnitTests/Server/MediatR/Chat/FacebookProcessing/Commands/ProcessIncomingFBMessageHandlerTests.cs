using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public class ProcessIncomingFBMessageHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<ProcessIncomingFBMessageHandler>> _loggerMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ProcessIncomingFBMessageHandler _handler;

        public ProcessIncomingFBMessageHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<ProcessIncomingFBMessageHandler>>();
            _mediatorMock = new Mock<IMediator>();
            _handler = new ProcessIncomingFBMessageHandler(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object);
        }

        [Fact]
        public async Task Handle_NoCompanyId_LogsAndReturns()
        {
            var eventJson = JsonDocument.Parse("""{ "message": { "text": "hi" } }""").RootElement;

            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByPageIdAsync("page1"))
                .ReturnsAsync((FacebookSettingsModel?)null);

            var result = await _handler.Handle(
                new ProcessIncomingFBMessageCommand("page1", eventJson), default);

            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ValidMessage_DispatchesProcessMessageCommand()
        {
            var eventJson = JsonDocument.Parse("""
            {
                "sender": { "id": "user123" },
                "message": { "mid": "msg789", "text": "Hello!" }
            }
            """).RootElement;

            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByPageIdAsync("page1"))
                .ReturnsAsync(new FacebookSettingsModel
                {
                    Id = "fb-settings-id",
                    CompanyId = "company1"
                });


            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ProcessMessageCommand>(), default))
                .ReturnsAsync(Unit.Value);

            var result = await _handler.Handle(
                new ProcessIncomingFBMessageCommand("page1", eventJson), default);

            _mediatorMock.Verify(m =>
                m.Send(It.Is<ProcessMessageCommand>(cmd =>
                    cmd.CompanyId == "company1" &&
                    cmd.SenderId == "user123" &&
                    cmd.ProviderMessageId == "msg789" &&
                    cmd.MessageText == "Hello!" &&
                    cmd.Source == "Facebook"), default),
                Times.Once);


            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_InvalidStructure_LogsAndReturns()
        {
            var eventJson = JsonDocument.Parse("""{ "something_else": true }""").RootElement;

            _unitOfWorkMock.Setup(u => u.FacebookSettings.GetSettingsByPageIdAsync("page1"))
                .ReturnsAsync(new FacebookSettingsModel
                {
                    Id = "fb-settings-id",
                    CompanyId = "company1"
                });

            var result = await _handler.Handle(
                new ProcessIncomingFBMessageCommand("page1", eventJson), default);

            _loggerMock.VerifyLog(LogLevel.Warning, Times.Once());
            Assert.Equal(Unit.Value, result);
        }
    }

    internal static class LoggerExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times)
        {
            logger.Verify(x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), times);
        }
    }
}