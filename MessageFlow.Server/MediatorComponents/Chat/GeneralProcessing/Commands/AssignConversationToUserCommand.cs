using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record AssignConversationToUserCommand(string ConversationId, string UserId) : IRequest<(bool Success, string ErrorMessage)>;
}
