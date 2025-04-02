using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record CreateCompanyCommand(CompanyDTO CompanyDto) : IRequest<(bool success, string errorMessage)>;
}
