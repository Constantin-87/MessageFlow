using MediatR;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record DeleteCompanyMetadataCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
