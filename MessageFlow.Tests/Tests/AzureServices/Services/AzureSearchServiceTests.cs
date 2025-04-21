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
using Moq.Protected;

namespace MessageFlow.Tests.Tests.AzureServices.Services;

public class AzureSearchServiceTests
{
    private readonly Mock<IAzureBlobStorageService> _blobStorageMock = new();
    private readonly Mock<ILogger<IAzureSearchService>> _loggerMock = new();

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
        await service.UploadDocumentsToIndexAsync("company1", new() { dto });

        _loggerMock.VerifyLogContains(LogLevel.Warning, "Empty content");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_SkipsNullUrl()
    {
        var dto = new ProcessedPretrainDataDTO { Id = "x", FileUrl = null, CompanyId = "company1" };

        var service = new AzureSearchService("https://test.search.windows.net", "fake-key", _blobStorageMock.Object, _loggerMock.Object);
        await service.UploadDocumentsToIndexAsync("company1", new() { dto });

        _loggerMock.VerifyLogContains(LogLevel.Warning, "Skipping file");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_LogsError_WhenBlobThrows()
    {
        var blobMock = new Mock<IAzureBlobStorageService>();
        blobMock.Setup(b => b.DownloadFileContentAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Blob error"));

        var dto = new ProcessedPretrainDataDTO { Id = "x", FileUrl = "some-url", CompanyId = "company1" };

        var service = new AzureSearchService("https://test.search.windows.net", "fake-key", blobMock.Object, _loggerMock.Object);
        await service.UploadDocumentsToIndexAsync("company1", new() { dto });

        _loggerMock.VerifyLogContains(LogLevel.Error, "Error processing file");
    }

    [Fact]
    public async Task UploadDocumentsToIndexAsync_LogsFailure_WhenUploadResultFails()
    {
        var dto = new ProcessedPretrainDataDTO
        {
            Id = "x",
            FileUrl = "url",
            FileDescription = "desc",
            CompanyId = "company1",
            ProcessedAt = DateTime.UtcNow
        };

        var blobMock = new Mock<IAzureBlobStorageService>();
        blobMock.Setup(b => b.DownloadFileContentAsync(It.IsAny<string>())).ReturnsAsync("data");

        var logger = new Mock<ILogger<IAzureSearchService>>();

        var fakeResult = SearchModelFactory.IndexingResult("x", "fail", false, 200);
        var fakeResponse = SearchModelFactory.IndexDocumentsResult(new[] { fakeResult });

        var searchClient = new Mock<SearchClient>();
        searchClient.Setup(s => s.UploadDocumentsAsync(It.IsAny<IEnumerable<Dictionary<string, object>>>(), null, default))
            .ReturnsAsync(Response.FromValue(fakeResponse, Mock.Of<Response>()));

        var serviceMock = new Mock<AzureSearchService>("https://test.search.windows.net", "fake-key", blobMock.Object, logger.Object)
        {
            CallBase = true
        };

        serviceMock.Protected().Setup<SearchClient>("GetSearchClient", ItExpr.IsAny<string>()).Returns(searchClient.Object);

        await serviceMock.Object.UploadDocumentsToIndexAsync("company1", new() { dto });

        logger.VerifyLogContains(LogLevel.Error, "Failed to index document with key");
    }

    [Fact]
    public async Task CreateCompanyIndexAsync_DeletesAndCreatesIndex()
    {
        var mockIndexClient = new Mock<SearchIndexClient>(MockBehavior.Strict, new Uri("https://mock"), new AzureKeyCredential("mock"));
        mockIndexClient.Setup(c => c.GetIndexNamesAsync(default)).Returns(GetMockIndexNamesAsync("company_company1_index"));
        mockIndexClient.Setup(c => c.DeleteIndexAsync("company_company1_index", default)).ReturnsAsync(Mock.Of<Response>());
        mockIndexClient.Setup(c => c.CreateIndexAsync(It.IsAny<SearchIndex>(), default)).ReturnsAsync(Mock.Of<Response<SearchIndex>>());

        var serviceMock = new Mock<AzureSearchService>("https://mock", "fake-key", Mock.Of<IAzureBlobStorageService>(), Mock.Of<ILogger<IAzureSearchService>>())
        {
            CallBase = true
        };

        serviceMock.Protected().Setup<SearchIndexClient>("GetIndexClient").Returns(mockIndexClient.Object);

        await serviceMock.Object.CreateCompanyIndexAsync("company1");

        mockIndexClient.Verify(x => x.DeleteIndexAsync("company_company1_index", default), Times.Once);
        mockIndexClient.Verify(x => x.CreateIndexAsync(It.IsAny<SearchIndex>(), default), Times.Once);
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