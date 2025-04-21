namespace MessageFlow.AzureServices.Interfaces
{
    public interface IAzureBlobStorageService
    {
        Task<string> GetAllCompanyRagDataFilesAsync(string companyId);
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string companyId);
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<string> DownloadFileContentAsync(string fileUrl);
        Task<Stream> DownloadFileAsStreamAsync(string fileUrl);
    }
}