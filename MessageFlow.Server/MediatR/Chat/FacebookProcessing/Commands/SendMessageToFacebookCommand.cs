using MediatR;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public record SendMessageToFacebookCommand(
        string RecipientId,
        string MessageText,
        string CompanyId,
        string LocalMessageId
    ) : IRequest<bool>;
}
