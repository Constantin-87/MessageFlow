using MediatR;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public record SendMessageToWhatsAppCommand(
        string RecipientPhoneNumber,
        string MessageText,
        string CompanyId,
        string LocalMessageId
    ) : IRequest<Unit>;
}
