using MediatR;
using System.Text.Json;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers
{
    public class ProcessFacebookWebhookEventHandler : IRequestHandler<ProcessFacebookWebhookEventCommand, Unit>
    {
        private readonly ILogger<ProcessFacebookWebhookEventHandler> _logger;
        private readonly IMediator _mediator;

        public ProcessFacebookWebhookEventHandler(ILogger<ProcessFacebookWebhookEventHandler> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(ProcessFacebookWebhookEventCommand request, CancellationToken cancellationToken)
        {
            var entry = request.Entry;

            if (!entry.TryGetProperty("messaging", out var messagingEvents))
                return Unit.Value;

            foreach (var messagingEvent in messagingEvents.EnumerateArray())
            {
                if (messagingEvent.TryGetProperty("delivery", out var delivery))
                {
                    if (delivery.TryGetProperty("mids", out var mids))
                    {
                        foreach (var mid in mids.EnumerateArray())
                        {
                            var statusElement = JsonDocument.Parse($"{{\"id\":\"{mid.GetString()}\",\"status\":\"delivered\"}}").RootElement;
                            await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));
                        }
                    }
                }
                else if (messagingEvent.TryGetProperty("read", out var read))
                {
                    var senderId = messagingEvent.GetProperty("sender").GetProperty("id").GetString();
                    var recipientId = messagingEvent.GetProperty("recipient").GetProperty("id").GetString();
                    await _mediator.Send(new HandleFacebookReadEventCommand(read, senderId, recipientId));
                }
                else if (messagingEvent.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean())
                    {
                        _logger.LogInformation($"Ignoring echo message with ID: {messageElement.GetProperty("mid").GetString()}");
                        continue;
                    }

                    var pageId = entry.GetProperty("id").GetString();
                    await _mediator.Send(new ProcessIncomingFBMessageCommand(pageId, messagingEvent));
                }
            }

            return Unit.Value;
        }
    }
}
