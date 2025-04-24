using MediatR;
using MessageFlow.Server.DataTransferObjects.Internal;

namespace MessageFlow.Server.MediatR.Chat.AiBotProcessing.Commands
{
    public record HandleUserQueryCommand(
        string UserQuery,
        string CompanyId,
        string ConversationId
    ) : IRequest<UserQueryResponseDTO>;
}