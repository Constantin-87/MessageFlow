using MediatR;

namespace MessageFlow.Server.MediatR.CompanyManagement.Queries
{
    public record GetCompanyMetadataQuery(string CompanyId) : IRequest<(bool success, string metadata, string errorMessage)>;
}