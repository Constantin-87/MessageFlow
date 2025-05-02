using MediatR;
using System.Text.Json;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.Helpers.Interfaces;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers
{
    public class SendMessageToFacebookHandler : IRequestHandler<SendMessageToFacebookCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendMessageToFacebookHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMessageSenderHelper _sender;

        public SendMessageToFacebookHandler(
            IUnitOfWork unitOfWork,
            ILogger<SendMessageToFacebookHandler> logger,
            IMediator mediator,
            IMessageSenderHelper sender)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
            _sender = sender;
        }

        public async Task<bool> Handle(SendMessageToFacebookCommand request, CancellationToken cancellationToken)
        {
            var facebookSettings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(request.CompanyId);

            if (facebookSettings == null)
            {
                _logger.LogWarning($"Facebook settings not found for company ID {request.CompanyId}.");
                return false;
            }

            var url = "https://graph.facebook.com/v11.0/me/messages";
            var payload = new
            {
                recipient = new { id = request.RecipientId },
                messaging_type = "RESPONSE",
                message = new { text = request.MessageText }
            };

            var response = await _sender.SendMessageAsync(url, payload, facebookSettings.AccessToken, _logger);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseBody);
                var facebookMessageId = responseJson.RootElement.GetProperty("message_id").GetString();

                var message = await _unitOfWork.Messages.GetMessageByIdAsync(request.LocalMessageId);

                if (message != null)
                {
                    message.ProviderMessageId = facebookMessageId;
                    await _unitOfWork.Messages.UpdateEntityAsync(message);
                    await _unitOfWork.SaveChangesAsync();
                }

                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send Facebook message: {responseBody}");

                try
                {

                    var errorDetails = JsonDocument.Parse(responseBody).RootElement;
                    var errorMessage = errorDetails.GetProperty("error").GetProperty("message").GetString();

                    var statusElement = JsonDocument.Parse(
                        $"{{\"id\":\"{request.LocalMessageId}\",\"status\":\"error\",\"errors\":[{{\"message\":\"{errorMessage}\"}}]}}"
                    ).RootElement;

                    await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Facebook error response.");
                    return false;
                }
            }
        }
    }
}