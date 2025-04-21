using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Tests.Tests.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public class ProcessFacebookWebhookEventHandlerTests
    {
        private readonly Mock<ILogger<ProcessFacebookWebhookEventHandler>> _loggerMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ProcessFacebookWebhookEventHandler _handler;

        public ProcessFacebookWebhookEventHandlerTests()
        {
            _loggerMock = new Mock<ILogger<ProcessFacebookWebhookEventHandler>>();
            _mediatorMock = new Mock<IMediator>();
            _handler = new ProcessFacebookWebhookEventHandler(_loggerMock.Object, _mediatorMock.Object);
        }

        [Fact]
        public async Task Handle_MissingMessaging_ReturnsUnit()
        {
            var entry = JsonDocument.Parse("""{"id":"page1"}""").RootElement;
            var command = new ProcessFacebookWebhookEventCommand(entry);

            var result = await _handler.Handle(command, default);
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ProcessesDeliveryEvents()
        {
            var entry = JsonDocument.Parse("""
            {
                "id": "page1",
                "messaging": [{
                    "delivery": {
                        "mids": ["mid1", "mid2"]
                    }
                }]
            }
            """).RootElement;

            var command = new ProcessFacebookWebhookEventCommand(entry);

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), default))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, default);

            _mediatorMock.Verify(x => x.Send(It.IsAny<ProcessMessageStatusUpdateCommand>(), default), Times.Exactly(2));
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ProcessesReadEvents()
        {
            var entry = JsonDocument.Parse("""
            {
                "id": "page1",
                "messaging": [{
                    "read": {"watermark": 123456},
                    "sender": { "id": "sender1" },
                    "recipient": { "id": "recipient1" }
                }]
            }
            """).RootElement;

            var command = new ProcessFacebookWebhookEventCommand(entry);

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<HandleFacebookReadEventCommand>(), default))
                .ReturnsAsync(Unit.Value);

            var result = await _handler.Handle(command, default);

            _mediatorMock.Verify(x => x.Send(It.IsAny<HandleFacebookReadEventCommand>(), default), Times.Once);
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_IgnoresEchoMessages()
        {
            var entry = JsonDocument.Parse("""
            {
                "id": "page1",
                "messaging": [{
                    "message": {
                        "mid": "m123",
                        "is_echo": true
                    }
                }]
            }
            """).RootElement;

            var command = new ProcessFacebookWebhookEventCommand(entry);

            var result = await _handler.Handle(command, default);

            _mediatorMock.Verify(x => x.Send(It.IsAny<IRequest<Unit>>(), default), Times.Never);
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_ProcessesIncomingMessage()
        {
            var entry = JsonDocument.Parse("""
            {
                "id": "page1",
                "messaging": [{
                    "message": {
                        "mid": "m123"
                    }
                }]
            }
            """).RootElement;

            var command = new ProcessFacebookWebhookEventCommand(entry);

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ProcessIncomingFBMessageCommand>(), default))
                .ReturnsAsync(Unit.Value);

            var result = await _handler.Handle(command, default);

            _mediatorMock.Verify(x => x.Send(It.IsAny<ProcessIncomingFBMessageCommand>(), default), Times.Once);
            Assert.Equal(Unit.Value, result);
        }
    }
}
