using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands
{
    public record SendMessageToWhatsAppCommand(
        string RecipientPhoneNumber,
        string MessageText,
        string CompanyId,
        string LocalMessageId
    ) : IRequest<Unit>;
}
