using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetCompanyPretrainingFilesQuery(string CompanyId) : IRequest<(bool success, List<ProcessedPretrainDataDTO> files, string errorMessage)>;
}
