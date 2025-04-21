using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MessageFlow.AzureServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageFlow.AzureServices.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "company-files";
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger, BlobServiceClient blobServiceClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _blobServiceClient = blobServiceClient ?? new BlobServiceClient(configuration["azure-storage-account-conn-string"]
                ?? throw new InvalidOperationException("Azure Blob Storage connection string is missing."));
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string companyId)
        {
            try
            {
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
                _logger.LogError($"Error uploading file: {ex.Message}");
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
                Console.WriteLine($"📂 Container Name: {_containerName}");
                Console.WriteLine($"🔗 Raw URL: {fileUrl}");
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                Console.WriteLine($"✅ Acquired BlobContainerClient");

                // Extract the correct blob name without container prefix
                string fullBlobPath = new Uri(fileUrl).AbsolutePath.TrimStart('/');
                Console.WriteLine($"📄 Full Blob Path: {fullBlobPath}");

                string blobName = fullBlobPath.Replace($"{_containerName}/", "");
                blobName = Uri.UnescapeDataString(blobName);
                Console.WriteLine($"🧩 Parsed Blob Name: {blobName}");


                var blobClient = blobContainerClient.GetBlobClient(blobName);
                Console.WriteLine($"📦 Getting blob client for: {blobClient.Uri}");

                var response = await blobClient.DeleteIfExistsAsync();
                Console.WriteLine($"✅ DeleteIfExistsAsync response: {response.Value}");

                return response.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception thrown: {ex.Message}");
                _logger.LogError($"Error deleting file: {ex.Message}");
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
                Console.WriteLine($"[LOG] Starting download for URL: {fileUrl}");

                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                Console.WriteLine($"[LOG] Got container client for '{_containerName}'");

                string fullBlobPath = new Uri(fileUrl).AbsolutePath.TrimStart('/');
                string blobName = fullBlobPath.Replace($"{_containerName}/", "");
                Console.WriteLine($"[LOG] Extracted blob name: {blobName}");

                var blobClient = blobContainerClient.GetBlobClient(blobName);
                Console.WriteLine("[LOG] Got blob client");

                var existsResponse = await blobClient.ExistsAsync();
                Console.WriteLine($"[LOG] Blob exists: {existsResponse.Value}");

                if (!existsResponse.Value)
                {
                    _logger.LogError($"Blob not found: {blobName}");
                    return string.Empty;
                }

                var response = await blobClient.DownloadContentAsync();
                Console.WriteLine($"[LOG] Downloaded content length: {response?.Value.Content.ToStream().Length}");

                if (response?.Value?.Content == null)
                {
                    Console.WriteLine("[ERROR] Blob content is null");
                    return string.Empty;
                }

                var result = response.Value.Content.ToString();
                Console.WriteLine($"[LOG] Content as string: {result}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                Console.WriteLine($"[ERROR] Exception: {ex}");
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
                _logger.LogError($"Error downloading file as stream: {ex.Message}");
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
                    if (blobItem.Name.EndsWith(".json"))
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
                    _logger.LogError($"No JSON files found in {baseFolderPath}");
                    return string.Empty;
                }

                // Combine all JSON file contents into a single string
                return string.Join("\n", fileContents);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving files from {companyId}/CompanyRAGData/: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
