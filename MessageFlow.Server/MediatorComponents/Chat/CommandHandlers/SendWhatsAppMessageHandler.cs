using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Services.Interfaces;
using MessageFlow.Server.MediatorComponents.Chat.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.CommandHandlers
{
    public class SendWhatsAppMessageHandler : IRequestHandler<SendWhatsAppMessageCommand, bool>
    {
        private readonly IWhatsAppService _whatsAppService;

        public SendWhatsAppMessageHandler(IWhatsAppService whatsAppService)
        {
            _whatsAppService = whatsAppService;
        }

        public async Task<bool> Handle(SendWhatsAppMessageCommand request, CancellationToken cancellationToken)
        {
            await _whatsAppService.SendMessageToWhatsAppAsync(
                request.RecipientPhoneNumber, request.MessageText, request.CompanyId, request.LocalMessageId);
            return true;
        }
    }
}
