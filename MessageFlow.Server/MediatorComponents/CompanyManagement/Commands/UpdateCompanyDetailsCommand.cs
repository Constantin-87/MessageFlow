using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record UpdateCompanyDetailsCommand(CompanyDTO CompanyDto) : IRequest<(bool success, string errorMessage)>;
}
