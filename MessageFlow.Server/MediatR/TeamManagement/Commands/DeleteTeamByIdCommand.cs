using MediatR;

namespace MessageFlow.Server.MediatR.TeamManagement.Commands
{
    public record DeleteTeamByIdCommand(string TeamId) : IRequest<(bool success, string errorMessage)>;
}
