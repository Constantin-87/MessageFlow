using MediatR;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record DeleteCompanyCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
