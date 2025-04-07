using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record BroadcastUserDisconnectedCommand(string CompanyId, string ConnectionId) : IRequest<Unit>;
}
