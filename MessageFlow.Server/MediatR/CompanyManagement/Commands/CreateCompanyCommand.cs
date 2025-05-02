using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record CreateCompanyCommand(CompanyDTO CompanyDto) : IRequest<(bool success, string errorMessage)>;
}