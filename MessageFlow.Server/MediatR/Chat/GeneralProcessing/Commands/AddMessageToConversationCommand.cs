using MediatR;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record AddMessageToConversationCommand(
        Conversation Conversation,
        string SenderId,
        string MessageText,
        string ProviderMessageId
    ) : IRequest<Unit>;
}
