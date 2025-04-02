using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MessageFlow.AzureServices.Helpers;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.AzureServices.Services
{
    public class AzureSearchService : IAzureSearchService
    {
        private readonly SearchIndexClient _searchIndexClient;
        private readonly string _searchServiceApiKey;
        private readonly IAzureBlobStorageService _azureBlobStorageService;

        public AzureSearchService(string searchServiceEndpoint, string adminApiKey, IAzureBlobStorageService azureBlobStorageService)
        {
            _azureBlobStorageService = azureBlobStorageService;
            _searchServiceApiKey = adminApiKey;
            _searchIndexClient = new SearchIndexClient(new Uri(searchServiceEndpoint), new AzureKeyCredential(adminApiKey));
        }


        public async Task CreateCompanyIndexAsync(string companyId)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);

            await foreach (var existingIndexName in _searchIndexClient.GetIndexNamesAsync())
            {
                if (existingIndexName == indexName)
                {
                    Console.WriteLine($"Index {indexName} already exists. Deleting...");

                    // ✅ Delete the existing index before proceeding
                    await _searchIndexClient.DeleteIndexAsync(indexName);

                    Console.WriteLine($"Index {indexName} deleted successfully.");
                    break; // No need to continue checking further
                }
            }

            // ✅ Continue execution (creating a new index)
            Console.WriteLine($"Creating new index: {indexName}");
            // Proceed with index creation logic here

            // 🔹 Define the index schema explicitly
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

            await _searchIndexClient.CreateIndexAsync(definition);
            Console.WriteLine($"✅ Index {indexName} created with required key field.");

        }


        public async Task UploadDocumentsToIndexAsync(string companyId, List<ProcessedPretrainDataDTO> processedFiles)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);

            var searchClient = new SearchClient(
                _searchIndexClient.Endpoint,
                indexName,
                new AzureKeyCredential(_searchServiceApiKey)
            );

            if (processedFiles == null || processedFiles.Count == 0)
            {
                Console.WriteLine($"⚠️ No documents to upload for index {indexName}");
                return;
            }

            var documents = new List<Dictionary<string, object>>();

            try
            {
                foreach (var file in processedFiles)
                {
                    if (string.IsNullOrEmpty(file.FileUrl))
                    {
                        Console.WriteLine($"⚠️ Skipping file {file.Id} - No associated FileUrl.");
                        continue;
                    }

                    try
                    {
                        // 🔹 Retrieve file content from Azure Blob Storage
                        string jsonContent = await _azureBlobStorageService.DownloadFileContentAsync(file.FileUrl);

                        if (string.IsNullOrEmpty(jsonContent))
                        {
                            Console.WriteLine($"⚠️ Skipping file {file.Id} - Empty content retrieved.");
                            continue;
                        }

                        //// 🔹 Convert JSON content to a structured dictionary
                        //var parsedContent = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                        //if (parsedContent == null)
                        //{
                        //    Console.WriteLine($"⚠️ Skipping file {file.Id} - Failed to deserialize JSON.");
                        //    continue;
                        //}

                        // 🔹 Prepare document for indexing
                        var document = new Dictionary<string, object>
                        {
                            { "document_id", file.Id },
                            { "file_description", file.FileDescription },
                            { "company_id", file.CompanyId },
                            { "content", jsonContent }, // Full parsed JSON content
                            { "processed_at", file.ProcessedAt }
                        };

                        documents.Add(document);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🚨 Error processing file {file.Id} from {file.FileUrl}: {ex.Message}");
                    }
                }

                if (documents.Count == 0)
                {
                    Console.WriteLine($"⚠️ No valid documents to upload for index {indexName}");
                    return;
                }

                // 🔹 Upload structured documents to Azure Search
                var response = await searchClient.UploadDocumentsAsync(documents);
                Console.WriteLine($"✅ Uploaded {documents.Count} documents to index {indexName}");

                // 🔍 Log response details
                Console.WriteLine($"🔎 Response Status: {response.GetRawResponse().Status}");

                foreach (var result in response.Value.Results)
                {
                    if (!result.Succeeded)
                    {
                        Console.WriteLine($"❌ Failed to index document with key: {result.Key}");
                    }
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"🚨 Azure Search API Error: {ex.Message}");
                Console.WriteLine($"🔎 Response Code: {ex.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 General Error uploading documents: {ex.Message}");
            }
        }





        //public async Task UploadDocumentsToIndexAsync(int companyId, Dictionary<string, object> documents)
        //{
        //    string indexName = SearchIndexHelper.GetIndexName(companyId);

        //    var searchClient = new SearchClient(
        //        _searchIndexClient.Endpoint,
        //        indexName,
        //        new AzureKeyCredential(_searchServiceApiKey)
        //    );

        //    if (documents == null || documents.Count == 0)
        //    {
        //        Console.WriteLine($"⚠️ No documents to upload for index {indexName}");
        //        return;
        //    }

        //    // ✅ Ensure the root document has an `id`
        //    if (!documents.ContainsKey("id") || string.IsNullOrEmpty(documents["id"]?.ToString()))
        //    {
        //        documents["id"] = $"company-{companyId}"; // Generate a unique ID using company ID
        //        Console.WriteLine($"🛠 Assigned root `id`: {documents["id"]}");
        //    }

        //    try
        //    {
        //        var response = await searchClient.UploadDocumentsAsync(new List<Dictionary<string, object>> { documents });
        //        Console.WriteLine($"✅ Uploaded {documents.Count} documents to index {indexName}");

        //        // Log response details
        //        Console.WriteLine($"🔎 Response Status: {response.GetRawResponse().Status}");

        //        foreach (var result in response.Value.Results)
        //        {
        //            if (!result.Succeeded)
        //            {
        //                Console.WriteLine($"❌ Failed to index document with key: {result.Key}");
        //            }
        //        }
        //    }
        //    catch (RequestFailedException ex)
        //    {
        //        Console.WriteLine($"🚨 Azure Search API Error: {ex.Message}");
        //        Console.WriteLine($"🔎 Response Code: {ex.Status}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"🚨 General Error uploading documents: {ex.Message}");
        //    }
        //}




    }
}
