using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.Queries
{
    public record GetUsersForTeamQuery(string TeamId) : IRequest<List<ApplicationUserDTO>>;
}
