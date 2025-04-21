using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record UpdateCompanyEmailsCommand(List<CompanyEmailDTO> CompanyEmails) : IRequest<(bool success, string errorMessage)>;
}
