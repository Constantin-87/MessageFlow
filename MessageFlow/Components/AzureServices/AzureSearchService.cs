using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MessageFlow.Components.AzureServices.Helpers;

namespace MessageFlow.Components.AzureServices
{
    public class AzureSearchService
    {
        private readonly SearchIndexClient _searchIndexClient;
        private readonly string _searchServiceApiKey;

        public AzureSearchService(string searchServiceEndpoint, string adminApiKey)
        {
            _searchServiceApiKey = adminApiKey;
            _searchIndexClient = new SearchIndexClient(new Uri(searchServiceEndpoint), new AzureKeyCredential(adminApiKey));
        }


        public async Task CreateCompanyIndexAsync(int companyId, Dictionary<string, object> structuredFields)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);

            await foreach (var existingIndexName in _searchIndexClient.GetIndexNamesAsync())
            {
                if (existingIndexName == indexName)
                {
                    Console.WriteLine($"Index {indexName} already exists.");
                    return;
                }
            }

            // ✅ Use the helper to generate fields
            var fields = SearchIndexDefinitionHelper.GenerateIndexFields(structuredFields);

            var definition = new SearchIndex(indexName) { Fields = fields };
            await _searchIndexClient.CreateIndexAsync(definition);

            Console.WriteLine($"Index {indexName} created with custom fields.");
        }


        public async Task UploadDocumentsToIndexAsync(int companyId, Dictionary<string, object> documents)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);

            var searchClient = new SearchClient(
                _searchIndexClient.Endpoint,
                indexName,
                new AzureKeyCredential(_searchServiceApiKey)
            );

            if (documents == null || documents.Count == 0)
            {
                Console.WriteLine($"⚠️ No documents to upload for index {indexName}");
                return;
            }

            // ✅ Ensure the root document has an `id`
            if (!documents.ContainsKey("id") || string.IsNullOrEmpty(documents["id"]?.ToString()))
            {
                documents["id"] = $"company-{companyId}"; // Generate a unique ID using company ID
                Console.WriteLine($"🛠 Assigned root `id`: {documents["id"]}");
            }

            try
            {
                var response = await searchClient.UploadDocumentsAsync(new List<Dictionary<string, object>> { documents });
                Console.WriteLine($"✅ Uploaded {documents.Count} documents to index {indexName}");

                // Log response details
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




    }
}
