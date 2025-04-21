using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using MessageFlow.AzureServices.Services;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace MessageFlow.Tests.Tests.AzureServices.Services;

public class AzureSearchQueryServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<AzureSearchQueryService>> _loggerMock = new();

    public AzureSearchQueryServiceTests()
    {
        _configMock.Setup(c => c["azure-ai-search-url"]).Returns("https://test.search.windows.net");
        _configMock.Setup(c => c["azure-ai-search-key"]).Returns("test-key");
    }

    private SearchResults<SearchDocument> CreateMockSearchResults(Pageable<SearchResult<SearchDocument>> results)
    {
        return SearchModelFactory.SearchResults<SearchDocument>(
            values: results,
            totalCount: 1,
            facets: null,
            coverage: null,
            rawResponse: Mock.Of<Response>()
        );
    }

    [Fact]
    public async Task QueryIndexAsync_ReturnsResults_WhenQueryIsValid()
    {
        // Arrange
        var mockSearchClient = new Mock<SearchClient>();

        var document = new SearchDocument
        {
            { "document_id", "doc1" },
            { "file_description", "Test file" },
            { "processed_at", "2024-01-01T00:00:00Z" },
            { "content", "example content" }
        };

        var searchResult = SearchModelFactory.SearchResult(document, 1.0, null);
        var page = Page<SearchResult<SearchDocument>>.FromValues(new[] { searchResult }, null, Mock.Of<Response>());
        var pageable = Pageable<SearchResult<SearchDocument>>.FromPages(new[] { page });

        mockSearchClient.Setup(c => c.SearchAsync<SearchDocument>(
                It.IsAny<string>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(CreateMockSearchResults(pageable), Mock.Of<Response>()));


        var service = new AzureSearchQueryService(
            _configMock.Object,
            _loggerMock.Object,
            indexName => mockSearchClient.Object
        );

        // Act
        var results = await service.QueryIndexAsync("test", "123");

        // Assert
        Assert.Single(results);
        Assert.Equal("doc1", results[0].Id);
    }

    [Fact]
    public async Task QueryIndexAsync_ReturnsEmptyList_WhenQueryIsEmpty()
    {
        var service = new AzureSearchQueryService(_configMock.Object, _loggerMock.Object);
        var results = await service.QueryIndexAsync("", "123");
        Assert.Empty(results);
    }

    [Fact]
    public void ExtractContentFromDocument_ReturnsFlattenedText()
    {
        var service = new AzureSearchQueryService(_configMock.Object, _loggerMock.Object);
        var document = new SearchDocument
        {
            { "field1", "value1" },
            { "field2", JsonDocument.Parse("\"value2\"").RootElement }
        };

        var content = service.ExtractContentFromDocument(document);
        Assert.Contains("field1: value1", content);
        Assert.Contains("value2", content);
    }

    [Fact]
    public async Task QueryIndexAsync_HandlesMissingFields()
    {
        var mockSearchClient = new Mock<SearchClient>();

        var document = new SearchDocument();
        var searchResult = SearchModelFactory.SearchResult(document, 0.5, null);
        var page = Page<SearchResult<SearchDocument>>.FromValues(new[] { searchResult }, null, Mock.Of<Response>());
        var pageable = Pageable<SearchResult<SearchDocument>>.FromPages(new[] { page });

        mockSearchClient.Setup(c => c.SearchAsync<SearchDocument>(
                It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(CreateMockSearchResults(pageable), Mock.Of<Response>()));

        var service = new AzureSearchQueryService(
            _configMock.Object,
            _loggerMock.Object,
            indexName => mockSearchClient.Object
        );

        var results = await service.QueryIndexAsync("test", "123");

        Assert.Single(results);
        Assert.Equal("N/A", results[0].Id);
        Assert.Equal("N/A", results[0].FileDescription);
        Assert.Equal("N/A", results[0].ProcessedAt);
    }

    [Fact]
    public async Task QueryIndexAsync_LogsError_OnException()
    {
        var mockSearchClient = new Mock<SearchClient>();

        mockSearchClient.Setup(c => c.SearchAsync<SearchDocument>(
                It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Search failed"));

        var logger = new Mock<ILogger<AzureSearchQueryService>>();

        var service = new AzureSearchQueryService(
            _configMock.Object,
            logger.Object,
            indexName => mockSearchClient.Object
        );

        var results = await service.QueryIndexAsync("test", "123");

        Assert.Empty(results);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error querying Azure Search index")),
                It.Is<Exception>(ex => ex.Message.Contains("Search failed")),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }
}