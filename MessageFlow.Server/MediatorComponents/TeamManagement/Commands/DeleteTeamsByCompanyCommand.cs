using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.Commands
{
    public record DeleteTeamsByCompanyCommand(string CompanyId) : IRequest<bool>;
}
