using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record DeleteCompanyMetadataCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
