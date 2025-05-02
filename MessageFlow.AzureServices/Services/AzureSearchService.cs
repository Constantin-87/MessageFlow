using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MessageFlow.AzureServices.Helpers;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MessageFlow.AzureServices.Services
{
    public class AzureSearchService : IAzureSearchService
    {
        private readonly SearchIndexClient _searchIndexClient;
        private readonly string _searchServiceApiKey;
        private readonly IAzureBlobStorageService _azureBlobStorageService;
        private readonly ILogger<IAzureSearchService> _logger;

        public AzureSearchService(
            string searchServiceEndpoint,
            string adminApiKey,
            IAzureBlobStorageService azureBlobStorageService,
            ILogger<IAzureSearchService> logger)
        {
            _azureBlobStorageService = azureBlobStorageService;
            _searchServiceApiKey = adminApiKey;
            _searchIndexClient = new SearchIndexClient(new Uri(searchServiceEndpoint), new AzureKeyCredential(adminApiKey));
            _logger = logger;
        }

        public async Task CreateCompanyIndexAsync(string companyId)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);
            var indexClient = GetIndexClient();

            await foreach (var existingIndexName in indexClient.GetIndexNamesAsync())
            {
                if (existingIndexName == indexName)
                {
                    await indexClient.DeleteIndexAsync(indexName);
                    break;
                }
            }

            // Define the index schema
            var fields = new List<SearchField>
            {
                new SearchField("document_id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchField("file_description", SearchFieldDataType.String) { IsSearchable = true },
                new SearchField("company_id", SearchFieldDataType.String) { IsFilterable = true },
                new SearchField("content", SearchFieldDataType.String) { IsSearchable = true },
                new SearchField("processed_at", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true }
            };

            var definition = new SearchIndex(indexName)
            {
                Fields = fields
            };

            await indexClient.CreateIndexAsync(definition);
        }

        public async Task UploadDocumentsToIndexAsync(string companyId, List<ProcessedPretrainDataDTO> processedFiles)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);

            var searchClient = GetSearchClient(indexName);

            if (processedFiles == null || processedFiles.Count == 0)
            {
                _logger.LogWarning("No documents to upload for index {IndexName}", indexName);
                return;
            }

            var documents = new List<Dictionary<string, object>>();

            try
            {
                foreach (var file in processedFiles)
                {
                    if (string.IsNullOrEmpty(file.FileUrl))
                    {
                        _logger.LogWarning("Skipping file {FileId} - No associated FileUrl", file.Id);
                        continue;
                    }

                    try
                    {
                        // Retrieve file content from Azure Blob Storage
                        string jsonContent = await _azureBlobStorageService.DownloadFileContentAsync(file.FileUrl);

                        if (string.IsNullOrEmpty(jsonContent))
                        {
                            _logger.LogWarning("Skipping file {FileId} - Empty content retrieved", file.Id);
                            continue;
                        }

                        // Prepare document for indexing
                        var document = new Dictionary<string, object>
                        {
                            { "document_id", file.Id },
                            { "file_description", file.FileDescription },
                            { "company_id", file.CompanyId },
                            { "content", jsonContent },
                            { "processed_at", file.ProcessedAt }
                        };

                        documents.Add(document);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file {FileId} from {FileUrl}", file.Id, file.FileUrl);
                    }
                }

                if (documents.Count == 0)
                {
                    _logger.LogWarning("No valid documents to upload for index {IndexName}", indexName);
                    return;
                }

                // Upload structured documents to Azure Search
                var response = await searchClient.UploadDocumentsAsync(documents);

                foreach (var result in response.Value.Results)
                {
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to index document with key: {Key}", result.Key);
                    }
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search API Error - Status: {StatusCode}", ex.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error uploading documents to index {IndexName}", indexName);
            }
        }

        protected internal virtual SearchClient GetSearchClient(string indexName)
        {
            return new SearchClient(
                _searchIndexClient.Endpoint,
                indexName,
                new AzureKeyCredential(_searchServiceApiKey)
            );
        }

        protected internal virtual SearchIndexClient GetIndexClient()
        {
            return _searchIndexClient;
        }
    }
}
