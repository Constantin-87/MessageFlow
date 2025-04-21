using MediatR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record AssignConversationToUserCommand(string ConversationId, string UserId) : IRequest<(bool Success, string ErrorMessage)>;
}
