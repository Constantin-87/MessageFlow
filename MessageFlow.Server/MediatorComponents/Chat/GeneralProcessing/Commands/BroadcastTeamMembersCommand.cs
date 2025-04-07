using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record BroadcastTeamMembersCommand(string CompanyId) : IRequest<Unit>;
}
