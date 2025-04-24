using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.UserManagement.Queries
{
    public class GetAllUsersQuery : IRequest<List<ApplicationUserDTO>> { }
}