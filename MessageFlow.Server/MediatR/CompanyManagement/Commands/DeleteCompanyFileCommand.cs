using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record DeleteCompanyFileCommand(ProcessedPretrainDataDTO File) : IRequest<bool>;
}
