using MediatR;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record SendAIResponseCommand(
        Conversation Conversation,
        string Response,
        string ProviderMessageId
    ) : IRequest<Unit>;
}
