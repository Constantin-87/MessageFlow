using MediatR;

namespace MessageFlow.Server.MediatR.UserManagement.Commands
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
