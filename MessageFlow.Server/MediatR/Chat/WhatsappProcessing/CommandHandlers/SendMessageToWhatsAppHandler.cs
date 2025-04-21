using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Helpers.Interfaces;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers
{
    public class SendMessageToWhatsAppHandler : IRequestHandler<SendMessageToWhatsAppCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<SendMessageToWhatsAppHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMessageSenderHelper _sender;

        public SendMessageToWhatsAppHandler(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            ILogger<SendMessageToWhatsAppHandler> logger,
            IMediator mediator,
            IMessageSenderHelper sender)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
            _mediator = mediator;
            _sender = sender;
        }

        public async Task<Unit> Handle(SendMessageToWhatsAppCommand request, CancellationToken cancellationToken)
        {
            var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(request.CompanyId);

            if (settings?.PhoneNumbers?.Any() != true)
            {
                return Unit.Value;
            }

            var phoneNumberInfo = settings.PhoneNumbers.FirstOrDefault();
            if (phoneNumberInfo == null)
            {
                return Unit.Value;
            }

            var url = $"https://graph.facebook.com/v17.0/{phoneNumberInfo.PhoneNumberId}/messages";
            var payload = new
            {
                messaging_product = "whatsapp",
                to = request.RecipientPhoneNumber,
                type = "text",
                text = new { body = request.MessageText }
            };

            var response = await _sender.SendMessageAsync(url, payload, settings.AccessToken, _logger);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();

                var messageId = JsonDocument.Parse(body).RootElement
                    .GetProperty("messages")[0]
                    .GetProperty("id")
                    .GetString();

                var message = await _unitOfWork.Messages.GetMessageByIdAsync(request.LocalMessageId);
                if (message != null)
                {
                    message.ProviderMessageId = messageId!;
                    await _unitOfWork.Messages.UpdateEntityAsync(message);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _chatHub.Clients.User(request.CompanyId)
                    .SendAsync("MessageStatusUpdated", request.LocalMessageId, "Sent");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();

                _logger.LogError($"Failed to send WhatsApp message: {body}");

                var errorMessage = JsonDocument.Parse(body).RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString();

                var errorJson = JsonDocument.Parse($$"""
                {
                  "id": "{{request.LocalMessageId}}",
                  "status": "error",
                  "errors": [{"message": "{{errorMessage}}"}]
                }
                """).RootElement;

                await _mediator.Send(new ProcessMessageStatusUpdateCommand(errorJson, "WhatsApp"));
                _logger.LogError($"Failed to send WhatsApp message to {request.RecipientPhoneNumber}");
            }
            return Unit.Value;
        }
    }
}
