using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MessageFlow.AzureServices.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MessageFlow.AzureServices.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "company-files"; // Change this to your container name

        public AzureBlobStorageService(IConfiguration configuration)
        {
            string storageConnectionString = configuration["azure-storage-account-conn-string"];
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage connection string is missing.");
            }

            _blobServiceClient = new BlobServiceClient(storageConnectionString);
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string companyId)
        {
            try
            {
                // Ensure the container exists
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                // Set unique file name per company
                string blobName = $"company_{companyId}/{fileName}";
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
                await blobClient.UploadAsync(fileStream, blobHttpHeaders);

                return blobClient.Uri.ToString(); // Return URL of the uploaded file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error uploading file: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Deletes a file from Azure Blob Storage.
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // Extract the correct blob name without container prefix
                string fullBlobPath = new Uri(fileUrl).AbsolutePath.TrimStart('/');
                string blobName = fullBlobPath.Replace($"{_containerName}/", ""); // Remove container prefix
                blobName = Uri.UnescapeDataString(blobName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                Console.WriteLine($"🔍 Deleting Blob: {blobName}");

                var response = await blobClient.DeleteIfExistsAsync();
                Console.WriteLine($"✅ Blob Deleted: {response.Value}");

                return response.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the content of a file from Azure Blob Storage.
        /// </summary>
        public async Task<string> DownloadFileContentAsync(string fileUrl)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // Extract the correct blob name without container prefix
                string fullBlobPath = new Uri(fileUrl).AbsolutePath.TrimStart('/');
                string blobName = fullBlobPath.Replace($"{_containerName}/", ""); // Remove container prefix

                var blobClient = blobContainerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    Console.WriteLine($"⚠️ Blob not found: {blobName}");
                    return string.Empty;
                }

                // Download file content
                var response = await blobClient.DownloadContentAsync();
                return response.Value.Content.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading file: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<Stream> DownloadFileAsStreamAsync(string fileUrl)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                string fullBlobPath = new Uri(fileUrl).AbsolutePath.TrimStart('/');
                string blobName = fullBlobPath.Replace($"{_containerName}/", "");

                var blobClient = blobContainerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Blob not found: {blobName}");
                }

                var response = await blobClient.DownloadContentAsync();
                return response.Value.Content.ToStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading file as stream: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all JSON files in the "CompanyRAGData" folder for a given company and returns their combined content.
        /// </summary>
        public async Task<string> GetAllCompanyRagDataFilesAsync(string companyId)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                string baseFolderPath = $"company_{companyId}/CompanyRAGData/";

                List<string> fileContents = new List<string>();

                // List all blobs in the CompanyRAGData folder
                await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync(prefix: baseFolderPath))
                {
                    if (blobItem.Name.EndsWith(".json")) // Ensure only JSON files are processed
                    {
                        var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);

                        // Download and read file content
                        var response = await blobClient.DownloadContentAsync();
                        string content = response.Value.Content.ToString();

                        fileContents.Add(content);
                    }
                }

                if (fileContents.Count == 0)
                {
                    Console.WriteLine($"⚠️ No JSON files found in {baseFolderPath}");
                    return string.Empty;
                }

                // Combine all JSON file contents into a single string
                return string.Join("\n", fileContents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving files from {companyId}/CompanyRAGData/: {ex.Message}");
                return string.Empty;
            }
        }



    }
}
