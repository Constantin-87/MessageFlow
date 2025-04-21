using MediatR;

namespace MessageFlow.Server.MediatR.TeamManagement.Commands
{
    public record DeleteTeamsByCompanyCommand(string CompanyId) : IRequest<bool>;
}
