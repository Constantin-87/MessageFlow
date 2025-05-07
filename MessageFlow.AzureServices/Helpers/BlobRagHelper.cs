using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using MessageFlow.AzureServices.Helpers.Interfaces;

namespace MessageFlow.AzureServices.Helpers
{
    public class BlobRagHelper : IBlobRagHelper
    {
        private readonly ILogger<BlobRagHelper> _logger;

        public BlobRagHelper(ILogger<BlobRagHelper> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetCompanyRagJsonContentsAsync(BlobContainerClient container, string baseFolderPath)
        {
            if (container == null)
            {
                _logger.LogWarning("Blob container is null.");
                return Enumerable.Empty<string>();
            }

            var result = new List<string>();
            await foreach (var blob in GetJsonBlobItemsAsync(container, baseFolderPath))
            {
                var content = await TryReadBlobContentAsync(container, blob.Name);
                if (content != null)
                    result.Add(content);
            }

            return result;
        }

        private async IAsyncEnumerable<BlobItem> GetJsonBlobItemsAsync(BlobContainerClient container, string prefix)
        {
            await foreach (var blob in container.GetBlobsAsync(prefix: prefix))
            {
                if (!string.IsNullOrWhiteSpace(blob.Name) && blob.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    yield return blob;
                }
                else
                {
                    _logger.LogDebug("Skipping blob: {BlobName}", blob.Name);
                }
            }
        }

        private async Task<string?> TryReadBlobContentAsync(BlobContainerClient container, string blobName)
        {
            try
            {
                var client = container.GetBlobClient(blobName);
                var response = await client.DownloadContentAsync();
                return response?.Value?.Content?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading blob {BlobName}", blobName);
                return null;
            }
        }
    }
}
