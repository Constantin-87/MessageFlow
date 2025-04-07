using MediatR;
using System.Text.Json;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.Helpers;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.CommandHandlers
{
    public class SendMessageToFacebookHandler : IRequestHandler<SendMessageToFacebookCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendMessageToFacebookHandler> _logger;
        private readonly IMediator _mediator;

        public SendMessageToFacebookHandler(IUnitOfWork unitOfWork, ILogger<SendMessageToFacebookHandler> logger, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
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

            var response = await MessageSenderHelper.SendMessageAsync(url, payload, facebookSettings.AccessToken, _logger);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseBody);
                var facebookMessageId = responseJson.RootElement.GetProperty("message_id").GetString();

                var message = await _unitOfWork.Messages.GetMessageByIdAsync(request.LocalMessageId);
                if (message != null)
                {
                    message.ProviderMessageId = facebookMessageId;
                    _unitOfWork.Messages.UpdateEntityAsync(message);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation($"Message sent to Facebook. Message ID: {facebookMessageId}");
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send Facebook message: {responseBody}");

                var errorDetails = JsonDocument.Parse(responseBody).RootElement;
                var errorMessage = errorDetails.GetProperty("error").GetProperty("message").GetString();

                var statusElement = JsonDocument.Parse(
                    $"{{\"id\":\"{request.LocalMessageId}\",\"status\":\"error\",\"errors\":[{{\"message\":\"{errorMessage}\"}}]}}"
                ).RootElement;

                await _mediator.Send(new ProcessMessageStatusUpdateCommand(statusElement, "Facebook"));
                return false;
            }
        }
    }
}
