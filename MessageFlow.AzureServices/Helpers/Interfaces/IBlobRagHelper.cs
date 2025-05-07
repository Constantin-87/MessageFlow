using Azure.Storage.Blobs;

namespace MessageFlow.AzureServices.Helpers.Interfaces
{
    public interface IBlobRagHelper
    {
        Task<IEnumerable<string>> GetCompanyRagJsonContentsAsync(BlobContainerClient container, string baseFolderPath);
    }
}