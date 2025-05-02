using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers
{
    public class ProcessIncomingWAMessageHandler : IRequestHandler<ProcessIncomingWAMessageCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<ProcessIncomingWAMessageHandler> _logger;

        public ProcessIncomingWAMessageHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<ProcessIncomingWAMessageHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Unit> Handle(ProcessIncomingWAMessageCommand request, CancellationToken cancellationToken)
        {
            var whatsAppSettings = await _unitOfWork.WhatsAppSettings
                .GetSettingsByBusinessAccountIdAsync(request.BusinessAccountId);

            var companyId = whatsAppSettings?.CompanyId;
            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning($"No WhatsApp settings found for BusinessAccountId {request.BusinessAccountId}");
                return Unit.Value;
            }

            foreach (var change in request.Changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                {
                    _logger.LogWarning($"No value found in change entry for BusinessAccountId {request.BusinessAccountId}");
                    continue;
                }

                // Handle delivery statuses
                if (value.TryGetProperty("statuses", out var statuses))
                {
                    var sortedStatuses = statuses.EnumerateArray()
                        .OrderBy(s => s.GetProperty("timestamp").GetString())
                        .ToList();

                    foreach (var status in sortedStatuses)
                    {
                        await _mediator.Send(new ProcessMessageStatusUpdateCommand(status, "WhatsApp"), cancellationToken);
                    }
                }

                // Handle new messages
                if (value.TryGetProperty("contacts", out var contacts))
                {
                    foreach (var contact in contacts.EnumerateArray())
                    {
                        var senderId = contact.GetProperty("wa_id").GetString();
                        var username = contact.GetProperty("profile").GetProperty("name").GetString();

                        if (value.TryGetProperty("messages", out var messages))
                        {
                            foreach (var message in messages.EnumerateArray())
                            {
                                var messageText = message.GetProperty("text").GetProperty("body").GetString();
                                var providerMessageId = message.GetProperty("id").GetString();

                                await _mediator.Send(new ProcessMessageCommand(
                                    companyId,
                                    senderId,
                                    username,
                                    messageText,
                                    providerMessageId,
                                    "WhatsApp"
                                ), cancellationToken);
                            }
                        }
                    }
                }
            }

            return Unit.Value;
        }
    }
}