using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using MessageFlow.AzureServices.Helpers;
using MessageFlow.Shared.DTOs;
using Azure.Search.Documents.Indexes;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MessageFlow.AzureServices.Interfaces;

namespace MessageFlow.AzureServices.Services
{
    public class AzureSearchQueryService: IAzureSearchQueryService
    {
        private readonly SearchIndexClient _searchIndexClient;
        private readonly string _searchServiceApiKey;
        private readonly string _searchServiceEndpoint;
        private readonly ILogger<AzureSearchQueryService> _logger;

        // Added optional injected factory for testing
        private readonly Func<string, SearchClient>? _searchClientFactory;

        public AzureSearchQueryService(
            IConfiguration configuration,
            ILogger<AzureSearchQueryService> logger,
            Func<string, SearchClient>? searchClientFactory = null)
        {

            _searchServiceEndpoint = configuration["azure-ai-search-url"];
            _searchServiceApiKey = configuration["azure-ai-search-key"];
            _logger = logger;
            _searchClientFactory = searchClientFactory;

            if (string.IsNullOrEmpty(_searchServiceEndpoint) || string.IsNullOrEmpty(_searchServiceApiKey))
            {
                throw new InvalidOperationException("Azure Search configuration is missing.");
            }

            _searchIndexClient = new SearchIndexClient(new Uri(_searchServiceEndpoint), new AzureKeyCredential(_searchServiceApiKey));
        }

        public async Task<List<SearchResultDTO>> QueryIndexAsync(string query, string companyId)
        {
            string indexName = SearchIndexHelper.GetIndexName(companyId);

            var searchClient = _searchClientFactory?.Invoke(indexName)
            ?? new SearchClient(_searchIndexClient.Endpoint, indexName, new AzureKeyCredential(_searchServiceApiKey));

            var results = new List<SearchResultDTO>();
            try
            {
                // Formating the string
                query = query?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(query))
                {
                    _logger.LogWarning("Empty search query received for company {CompanyId}", companyId);
                    return results;
                }

                // Set safe SearchOptions
                var searchOptions = new SearchOptions
                {
                    Size = 10,  // Limit results to 10
                    QueryType = SearchQueryType.Simple, // Use 'Simple' instead of 'Full'
                    SearchMode = SearchMode.Any,  // Allow partial matches
                    IncludeTotalCount = true
                };

                var response = await searchClient.SearchAsync<SearchDocument>(query, searchOptions);

                foreach (var result in response.Value.GetResults())
                {
                    string content = ExtractContentFromDocument(result.Document);

                    // Handle missing fields
                    results.Add(new SearchResultDTO
                    {
                        Id = result.Document.TryGetValue("document_id", out var idValue) ? idValue.ToString() : "N/A",
                        FileDescription = result.Document.TryGetValue("file_description", out var descriptionValue) ? descriptionValue.ToString() : "N/A",
                        Content = content,
                        ProcessedAt = result.Document.TryGetValue("processed_at", out var timeValue) ? timeValue.ToString() : "N/A",
                        Score = result.Score
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying Azure Search index for company {CompanyId}", companyId);
            }

            return results;
        }

        public string ExtractContentFromDocument(SearchDocument document)
        {
            StringBuilder contentBuilder = new StringBuilder();

            foreach (var field in document)
            {
                if (field.Value is JsonElement element)
                {
                    contentBuilder.AppendLine(ParseJsonElement(element));
                }
                else if (field.Value is IEnumerable<object> list)
                {
                    foreach (var item in list)
                    {
                        if (item is JsonElement listElement)
                        {
                            contentBuilder.AppendLine(ParseJsonElement(listElement));
                        }
                        else
                        {
                            contentBuilder.AppendLine(item.ToString());
                        }
                    }
                }
                else
                {
                    contentBuilder.AppendLine($"{field.Key}: {field.Value}");
                }
            }

            return contentBuilder.ToString();
        }

        private string ParseJsonElement(JsonElement element)
        {
            StringBuilder builder = new StringBuilder();

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    builder.AppendLine($"{property.Name}: {ParseJsonElement(property.Value)}");
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    builder.AppendLine(ParseJsonElement(item));
                }
            }
            else
            {
                builder.AppendLine(element.ToString());
            }

            return builder.ToString();
        }
    }
}