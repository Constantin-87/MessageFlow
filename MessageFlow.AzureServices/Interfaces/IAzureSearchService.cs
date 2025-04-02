using MessageFlow.Shared.DTOs;

namespace MessageFlow.AzureServices.Interfaces
{
    public interface IAzureSearchService
    {
        Task CreateCompanyIndexAsync(string companyId);
        Task UploadDocumentsToIndexAsync(string companyId, List<ProcessedPretrainDataDTO> documents);
    }
}
