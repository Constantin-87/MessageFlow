using MediatR;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record GenerateCompanyMetadataCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
