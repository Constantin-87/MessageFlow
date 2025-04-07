using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record AddUserToGroupsCommand(ApplicationUserDTO ApplicationUser, string ConnectionId) : IRequest<Unit>;

}
