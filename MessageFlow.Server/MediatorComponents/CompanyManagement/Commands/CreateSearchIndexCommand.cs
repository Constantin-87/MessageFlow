using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record CreateSearchIndexCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
