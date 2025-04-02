using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.Commands
{
    public record AddTeamToCompanyCommand(TeamDTO TeamDto) : IRequest<(bool success, string errorMessage)>;
}
