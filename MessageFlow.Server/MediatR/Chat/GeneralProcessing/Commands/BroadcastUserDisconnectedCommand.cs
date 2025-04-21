using MediatR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record BroadcastUserDisconnectedCommand(string CompanyId, string ConnectionId) : IRequest<Unit>;
}