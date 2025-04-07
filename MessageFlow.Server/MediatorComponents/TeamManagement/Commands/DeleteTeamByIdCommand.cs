using MediatR;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.Commands
{
    public record DeleteTeamByIdCommand(string TeamId) : IRequest<(bool success, string errorMessage)>;
}
