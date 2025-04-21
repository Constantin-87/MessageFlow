using Azure.Search.Documents.Models;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.AzureServices.Interfaces
{
    public interface IAzureSearchQueryService
    {
        Task<List<SearchResultDTO>> QueryIndexAsync(string query, string companyId);
        string ExtractContentFromDocument(SearchDocument document);
    }
}