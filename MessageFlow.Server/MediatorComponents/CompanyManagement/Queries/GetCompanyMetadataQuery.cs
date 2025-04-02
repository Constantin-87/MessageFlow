using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetCompanyMetadataQuery(string CompanyId) : IRequest<(bool success, string metadata, string errorMessage)>;
}
