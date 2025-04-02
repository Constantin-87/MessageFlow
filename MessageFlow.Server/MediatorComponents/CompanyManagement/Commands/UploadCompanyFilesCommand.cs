using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Commands
{
    public record UploadCompanyFilesCommand(List<PretrainDataFileDTO> Files) : IRequest<(bool success, string errorMessage)>;
}
