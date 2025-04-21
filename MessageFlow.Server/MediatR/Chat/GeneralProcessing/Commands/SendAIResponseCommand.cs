using MediatR;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record SendAIResponseCommand(
        Conversation Conversation,
        string Response,
        string ProviderMessageId
    ) : IRequest<Unit>;
}