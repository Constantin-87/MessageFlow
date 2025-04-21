using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.AzureServices.Helpers.Interfaces
{
    public interface ICompanyDataHelper
    {
        Task<(List<ProcessedPretrainDataDTO>, List<string>)> ProcessUploadedFilesAsync(
            List<PretrainDataFileDTO> files,
            IDocumentProcessingService docService);

        (List<ProcessedPretrainDataDTO>, List<string>) GenerateStructuredCompanyMetadata(CompanyDTO company);
    }
}