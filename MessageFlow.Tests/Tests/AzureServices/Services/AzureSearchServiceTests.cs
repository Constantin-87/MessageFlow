using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.AzureServices.Services;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MessageFlow.Tests.Tests.AzureServices.Services;

public class AzureSearchServiceTests
{
    private readonly Mock<IAzureBlobStorageService> _blobStorageMock = new();
    private readonly Mock<ILogger<AzureSearchService>> _loggerMock = new();

    [Fact]
    public async Task UploadDocumentsToIndexAsync_LogsWarning_WhenListIsEmpty()
    {
        var service = new AzureSearchService(
            "https://test.search.windows.net",
            "fake-key",
            _blobStorageMock.Object,
            _loggerMock.Object
        );

        await service.UploadDocumentsToIndexAsync("company1", new());

        _loggerMock.VerifyLogContains(LogLevel.Warning, "No documents to upload");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_SkipsInvalidFiles()
    {
        _blobStorageMock.Setup(x => x.DownloadFileContentAsync(It.IsAny<string>())).ReturnsAsync("");
        var dto = new ProcessedPretrainDataDTO { Id = "x", FileUrl = "some-url", CompanyId = "company1" };

        var service = new AzureSearchService("https://test.search.windows.net", "fake-key", _blobStorageMock.Object, _loggerMock.Object);

        await service.UploadDocumentsToIndexAsync("company1", new List<ProcessedPretrainDataDTO> { dto });

        _loggerMock.VerifyLogContains(LogLevel.Warning, "Empty content");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_SkipsNullUrl()
    {
        var dto = new ProcessedPretrainDataDTO { Id = "x", FileUrl = null, CompanyId = "company1" };

        var service = new AzureSearchService("https://test.search.windows.net", "fake-key", _blobStorageMock.Object, _loggerMock.Object);

        await service.UploadDocumentsToIndexAsync("company1", new List<ProcessedPretrainDataDTO> { dto });

        _loggerMock.VerifyLogContains(LogLevel.Warning, "Skipping file");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_LogsError_WhenBlobThrows()
    {
        var blob = new Mock<IAzureBlobStorageService>();
        blob.Setup(b => b.DownloadFileContentAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Blob error"));

        var dto = new ProcessedPretrainDataDTO { Id = "x", FileUrl = "some-url", CompanyId = "company1" };
        var service = new AzureSearchService("https://test.search.windows.net", "fake-key", blob.Object, _loggerMock.Object);

        await service.UploadDocumentsToIndexAsync("company1", new() { dto });

        _loggerMock.VerifyLogContains(LogLevel.Error, "Error processing file");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_LogsFailure_WhenUploadResultFails()
    {
        var blob = new Mock<IAzureBlobStorageService>();
        blob.Setup(b => b.DownloadFileContentAsync(It.IsAny<string>())).ReturnsAsync("data");

        var dto = new ProcessedPretrainDataDTO
        {
            Id = "x",
            FileUrl = "url",
            FileDescription = "desc",
            CompanyId = "company1",
            ProcessedAt = DateTime.UtcNow
        };

        var logger = new Mock<ILogger<AzureSearchService>>();

        var fakeResult = SearchModelFactory.IndexingResult(
            key: "x",
            errorMessage: "Indexing failed",
            succeeded: false,
            status: 200
        );
        var fakeUploadResponse = SearchModelFactory.IndexDocumentsResult(new[] { fakeResult });

        var searchClientMock = new Mock<SearchClient>();
        searchClientMock
    .Setup(c => c.UploadDocumentsAsync(
        It.IsAny<IEnumerable<Dictionary<string, object>>>(),
        It.IsAny<IndexDocumentsOptions>(),
        It.IsAny<CancellationToken>()
    ))
    .ReturnsAsync(Response.FromValue(fakeUploadResponse, Mock.Of<Response>()));


        var service = new AzureSearchServiceProxy(
            new Mock<SearchIndexClient>().Object,
            "fake-key",
            blob.Object,
            logger.Object,
            searchClientMock.Object
        );

        await service.UploadDocumentsToIndexAsync("company1", new() { dto });

        logger.VerifyLogContains(LogLevel.Error, "Failed to index document with key");
    }

    [Fact]
    public async Task CreateCompanyIndexAsync_DeletesAndCreatesIndex()
    {
        // Arrange: Mock SearchIndexClient
        var clientMock = new Mock<SearchIndexClient>(MockBehavior.Strict, new Uri("https://mock"), new AzureKeyCredential("mock-key"));

        // Setup GetIndexNamesAsync to simulate existing index
        clientMock.Setup(c => c.GetIndexNamesAsync(It.IsAny<CancellationToken>()))
            .Returns(GetMockIndexNamesAsync("company_company1_index"));

        clientMock.Setup(c => c.DeleteIndexAsync("company_company1_index", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        clientMock.Setup(c => c.CreateIndexAsync(It.IsAny<SearchIndex>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<SearchIndex>>());

        // Create mock service using your proxy with overridden GetIndexClient
        var service = new AzureSearchServiceProxy(
            clientMock.Object,
            "fake-key",
            Mock.Of<IAzureBlobStorageService>(),
            new Mock<ILogger<AzureSearchService>>().Object,
            Mock.Of<SearchClient>()
        );

        // Act
        await service.CreateCompanyIndexAsync("company1");

        // Assert
        clientMock.Verify(x => x.DeleteIndexAsync("company_company1_index", It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(x => x.CreateIndexAsync(It.IsAny<SearchIndex>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    private static AsyncPageable<string> GetMockIndexNamesAsync(params string[] names)
    {
        var pages = new[] { Page<string>.FromValues(names, null, Mock.Of<Response>()) };
        return AsyncPageable<string>.FromPages(pages);
    }


}

public static class LoggerMockExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, string message)
    {
        mockLogger.Verify(logger => logger.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(message)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }
}

public class AzureSearchServiceProxy : AzureSearchService
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;

    public AzureSearchServiceProxy(
        SearchIndexClient indexClient,
        string key,
        IAzureBlobStorageService blob,
        ILogger<AzureSearchService> logger,
        SearchClient searchClient
    ) : base("https://test.search.windows.net", key, blob, logger)
    {
        _indexClient = indexClient;
        _searchClient = searchClient;
    }
    protected internal override SearchClient GetSearchClient(string indexName) => _searchClient;
    protected internal override SearchIndexClient GetIndexClient() => _indexClient;
}