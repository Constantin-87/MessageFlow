using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.CommandHandlers
{
    public class ProcessIncomingFBMessageHandler : IRequestHandler<ProcessIncomingFBMessageCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProcessIncomingFBMessageHandler> _logger;
        private readonly IMediator _mediator;

        public ProcessIncomingFBMessageHandler(
            IUnitOfWork unitOfWork,
            ILogger<ProcessIncomingFBMessageHandler> logger,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(ProcessIncomingFBMessageCommand request, CancellationToken cancellationToken)
        {
            var settings = await _unitOfWork.FacebookSettings.GetSettingsByPageIdAsync(request.PageId);
            var companyId = settings?.CompanyId;

            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning($"No Facebook settings found for Page ID {request.PageId}");
                return Unit.Value;
            }

            var eventData = request.EventData;

            if (eventData.TryGetProperty("sender", out var senderElement) &&
                eventData.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("mid", out var midElement))
            {
                var senderId = senderElement.GetProperty("id").GetString();
                var messageText = messageElement.GetProperty("text").GetString();
                var providerMessageId = midElement.GetString();

                var senderUserName = senderId; // Placeholder

                await _mediator.Send(new ProcessMessageCommand(
                    companyId, senderId, senderUserName, messageText, providerMessageId, "Facebook"), cancellationToken);
            }
            else
            {
                _logger.LogWarning($"Unhandled event type in Facebook webhook payload: {eventData}");
            }

            return Unit.Value;
        }
    }
}
