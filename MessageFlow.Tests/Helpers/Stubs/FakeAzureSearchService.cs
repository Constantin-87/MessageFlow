using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Tests.Helpers.Stubs
{
    public class FakeAzureSearchService : IAzureSearchService
    {
        public Task CreateCompanyIndexAsync(string companyId)
        {
            return Task.CompletedTask;
        }

        public Task UploadDocumentsToIndexAsync(string companyId, List<ProcessedPretrainDataDTO> documents)
        {
            return Task.CompletedTask;
        }
    }
}
