using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetUserByIdQuery : IRequest<ApplicationUserDTO?>
    {
        public string UserId { get; }

        public GetUserByIdQuery(string userId)
        {
            UserId = userId;
        }
    }
}
