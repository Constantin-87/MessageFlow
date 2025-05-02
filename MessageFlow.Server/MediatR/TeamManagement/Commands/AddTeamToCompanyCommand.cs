using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.TeamManagement.Commands
{
    public record AddTeamToCompanyCommand(TeamDTO TeamDto) : IRequest<(bool success, string errorMessage)>;
}