using MediatR;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetAvailableRolesQuery : IRequest<List<string>> { }
}
