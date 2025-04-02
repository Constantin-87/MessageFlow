using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Commands
{
    public class DeleteUserCommand : IRequest<bool>
    {
        public string UserId { get; }

        public DeleteUserCommand(string userId)
        {
            UserId = userId;
        }
    }
}
