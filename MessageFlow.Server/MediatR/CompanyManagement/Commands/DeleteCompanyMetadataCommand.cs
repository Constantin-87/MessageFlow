using MediatR;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record DeleteCompanyMetadataCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}