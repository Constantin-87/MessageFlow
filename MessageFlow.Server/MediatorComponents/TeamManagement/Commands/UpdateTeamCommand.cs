using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.Commands
{
    public record UpdateTeamCommand(TeamDTO TeamDto) : IRequest<(bool success, string errorMessage)>;
}
