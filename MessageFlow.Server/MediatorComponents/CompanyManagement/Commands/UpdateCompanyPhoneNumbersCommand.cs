using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record UpdateCompanyPhoneNumbersCommand(List<CompanyPhoneNumberDTO> CompanyPhoneNumbers) : IRequest<(bool success, string errorMessage)>;
}
