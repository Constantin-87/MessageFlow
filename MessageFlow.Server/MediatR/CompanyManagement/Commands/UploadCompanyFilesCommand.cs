using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record UploadCompanyFilesCommand(List<PretrainDataFileDTO> Files) : IRequest<(bool success, string errorMessage)>;
}