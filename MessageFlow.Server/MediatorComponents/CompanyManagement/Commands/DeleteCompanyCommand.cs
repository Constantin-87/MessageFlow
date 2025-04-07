using MediatR;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record DeleteCompanyCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
