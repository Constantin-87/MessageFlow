using MediatR;
using System.Text.Json;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.CommandHandlers
{
    public class HandleFacebookReadEventHandler : IRequestHandler<HandleFacebookReadEventCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HandleFacebookReadEventHandler> _logger;
        private readonly IMediator _mediator;

        public HandleFacebookReadEventHandler(
            IUnitOfWork unitOfWork,
            ILogger<HandleFacebookReadEventHandler> logger,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(HandleFacebookReadEventCommand request, CancellationToken cancellationToken)
        {
            var read = request.Read;
            var senderId = request.SenderId;
            var recipientId = request.RecipientId;

            if (!read.TryGetProperty("watermark", out var watermarkProperty))
            {
                _logger.LogWarning("No 'watermark' property found in the 'read' event.");
                return Unit.Value;
            }

            if (!long.TryParse(watermarkProperty.ToString(), out var watermarkUnix) ||
                watermarkUnix > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds() ||
                watermarkUnix < DateTimeOffset.MinValue.ToUnixTimeMilliseconds())
            {
                _logger.LogWarning($"Invalid watermark timestamp: {watermarkProperty}. Skipping processing.");
                return Unit.Value;
            }

            var watermarkTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(watermarkUnix).UtcDateTime;

            var facebookSettings = await _unitOfWork.FacebookSettings.GetSettingsByPageIdAsync(recipientId);
            if (facebookSettings == null)
            {
                _logger.LogWarning($"No Facebook settings found for Page ID {recipientId}.");
                return Unit.Value;
            }

            var conversation = await _unitOfWork.Conversations.GetConversationBySenderAndCompanyAsync(senderId, facebookSettings.CompanyId);
            if (conversation == null)
            {
                _logger.LogWarning($"No conversation found for sender {senderId} in company {facebookSettings.CompanyId}.");
                return Unit.Value;
            }

            var messagesToUpdate = await _unitOfWork.Messages.GetUnreadMessagesBeforeTimestampAsync(conversation.Id, watermarkTimestamp);

            foreach (var message in messagesToUpdate)
            {
                try
                {
                    var statusJson = $"{{\"id\":\"{message.ProviderMessageId}\",\"status\":\"read\",\"timestamp\":\"{watermarkUnix / 1000}\"}}";
                    var statusElement = JsonDocument.Parse(statusJson).RootElement;
                    await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating message ID {message.Id} to 'read': {ex.Message}");
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Marked {messagesToUpdate.Count} messages as read in conversation {conversation.Id} up to watermark timestamp {watermarkTimestamp}.");

            return Unit.Value;
        }
    }
}
