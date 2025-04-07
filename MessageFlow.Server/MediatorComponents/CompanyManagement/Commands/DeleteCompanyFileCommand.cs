using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record DeleteCompanyFileCommand(ProcessedPretrainDataDTO File) : IRequest<bool>;
}
