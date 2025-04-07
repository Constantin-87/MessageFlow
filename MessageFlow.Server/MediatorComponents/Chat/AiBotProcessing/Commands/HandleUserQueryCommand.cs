using MediatR;

namespace MessageFlow.Server.MediatorComponents.Chat.AiBotProcessing.Commands
{
    public record HandleUserQueryCommand(
        string UserQuery,
        string CompanyId,
        string ConversationId
    ) : IRequest<(bool Answered, string Response, string? TargetTeamId)>;

}
