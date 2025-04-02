using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetAvailableRolesQuery : IRequest<List<string>> { }
}
