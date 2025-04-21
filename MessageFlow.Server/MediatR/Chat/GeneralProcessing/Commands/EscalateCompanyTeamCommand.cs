using MediatR;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record EscalateCompanyTeamCommand(
        Conversation Conversation,
        string SenderId,
        string ProviderMessageId,
        string TargetTeamId,
        string TargetTeamName
    ) : IRequest<Unit>;
}