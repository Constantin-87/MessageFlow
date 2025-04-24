using MediatR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record BroadcastTeamMembersCommand(string CompanyId) : IRequest<Unit>;
}