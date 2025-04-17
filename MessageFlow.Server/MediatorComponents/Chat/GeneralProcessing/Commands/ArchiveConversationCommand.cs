using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record ArchiveConversationCommand(string CustomerId) : IRequest<(bool Success, string ErrorMessage)>;

}
