using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetAllUsersQuery : IRequest<List<ApplicationUserDTO>> { }
}
