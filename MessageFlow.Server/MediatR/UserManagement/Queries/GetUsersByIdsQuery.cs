using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.UserManagement.Queries
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