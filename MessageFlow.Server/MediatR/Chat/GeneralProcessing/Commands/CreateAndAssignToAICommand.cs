using MediatR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record CreateAndAssignToAICommand(
        string CompanyId,
        string SenderId,
        string Username,
        string MessageText,
        string ProviderMessageId,
        string Source
    ) : IRequest<Unit>;
}