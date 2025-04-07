using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands
{
    public record SendMessageToFacebookCommand(
        string RecipientId,
        string MessageText,
        string CompanyId,
        string LocalMessageId
    ) : IRequest<bool>;
}
