using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record LoadUserConversationsCommand(string UserId, string CompanyId, IClientProxy Caller) : IRequest<Unit>;
}
