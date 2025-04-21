using MediatR;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record HandleAIConversationCommand(
        Conversation Conversation,
        string MessageText,
        string ProviderMessageId
    ) : IRequest<Unit>;
}