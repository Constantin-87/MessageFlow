using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetAllUsersQuery : IRequest<List<ApplicationUserDTO>> { }
}
