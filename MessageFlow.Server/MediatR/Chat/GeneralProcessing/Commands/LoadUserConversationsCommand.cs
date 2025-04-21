using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record LoadUserConversationsCommand(string UserId, string CompanyId, IClientProxy Caller) : IRequest<Unit>;
}