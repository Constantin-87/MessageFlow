using MessageFlow.Infrastructure.Mediator.Commands.Chat;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Handlers.Chat
{
    public class SendWhatsAppMessageCommandHandler : IRequestHandler<SendWhatsAppMessageCommand, bool>
    {
        private readonly IWhatsAppService _whatsAppService;

        public SendWhatsAppMessageCommandHandler(IWhatsAppService whatsAppService)
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
