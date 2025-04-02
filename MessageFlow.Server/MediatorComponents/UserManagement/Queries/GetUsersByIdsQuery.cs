using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetUsersByIdsQuery : IRequest<List<ApplicationUserDTO>>
    {
        public List<string> UserIds { get; }

        public GetUsersByIdsQuery(List<string> userIds)
        {
            UserIds = userIds;
        }
    }
}
