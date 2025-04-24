using MessageFlow.AzureServices.Interfaces;

namespace MessageFlow.Tests.Helpers.Stubs
{
    public class FakeAzureBlobStorageService : IAzureBlobStorageService
    {
        public Task<string> GetAllCompanyRagDataFilesAsync(string companyId)
            => Task.FromResult("mock content");

        public Task<bool> DeleteFileAsync(string fileUrl)
            => Task.FromResult(true);

        public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string companyId)
            => Task.FromResult($"https://fake.blob.core.windows.net/{companyId}/{fileName}");

        public Task<string> DownloadFileContentAsync(string fileUrl)
            => Task.FromResult("mock file content");

        public Task<Stream> DownloadFileAsStreamAsync(string fileUrl)
        {
            var mockContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("mock stream content"));
            return Task.FromResult<Stream>(mockContent);
        }
    }
}