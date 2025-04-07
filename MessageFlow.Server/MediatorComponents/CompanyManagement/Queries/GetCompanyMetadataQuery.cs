using MediatR;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetCompanyMetadataQuery(string CompanyId) : IRequest<(bool success, string metadata, string errorMessage)>;
}
