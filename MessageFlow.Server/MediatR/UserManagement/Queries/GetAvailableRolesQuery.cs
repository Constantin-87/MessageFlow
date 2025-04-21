using MediatR;

namespace MessageFlow.Server.MediatR.UserManagement.Queries
{
    public class GetAvailableRolesQuery : IRequest<List<string>> { }
}
