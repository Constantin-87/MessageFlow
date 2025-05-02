using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.UserManagement.Queries
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