using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record CloseAndAnonymizeChatCommand(string CustomerId) : IRequest<(bool Success, string ErrorMessage)>;
}
