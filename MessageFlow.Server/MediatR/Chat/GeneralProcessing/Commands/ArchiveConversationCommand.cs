using MediatR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record ArchiveConversationCommand(string CustomerId) : IRequest<(bool Success, string ErrorMessage)>;

}
