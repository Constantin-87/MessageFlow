using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.TeamManagement.Queries
{
    public record GetUsersForTeamQuery(string TeamId) : IRequest<List<ApplicationUserDTO>>;
}