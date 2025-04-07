using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.Queries
{
    public record GetTeamsForCompanyQuery(string CompanyId) : IRequest<List<TeamDTO>>;
}
