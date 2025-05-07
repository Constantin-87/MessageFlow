using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MessageFlow.AzureServices.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace MessageFlow.Tests.UnitTests.AzureServices.Helpers;

public class BlobRagHelperTests
{
    private readonly Mock<ILogger<BlobRagHelper>> _loggerMock = new();
    private readonly Mock<BlobContainerClient> _containerClientMock = new();

    public BlobRagHelperTests(ITestOutputHelper output)
    {
        Console.SetOut(new TestOutputTextWriter(output));
    }

    [Fact]
    public async Task GetCompanyRagJsonContentsAsync_ReturnsJsonContent()
    {
        var blobName = "mockfile.json";
        var expectedContent = "{\"mock\":true}";

        var blobItem = BlobsModelFactory.BlobItem(name: blobName);

        _containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItem));

        _containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(new FakeBlobClient(expectedContent));

        var helper = new BlobRagHelper(_loggerMock.Object);
        var result = (await helper.GetCompanyRagJsonContentsAsync(_containerClientMock.Object, "company_123/CompanyRAGData/")).ToList();

        Assert.Single(result);
        Assert.Equal(expectedContent, result[0]);
    }

    [Fact]
    public async Task GetCompanyRagJsonContentsAsync_LogsError_OnDownloadFailure()
    {
        const string blobName = "company_123/CompanyRAGData/file1.json";
        var blobItem = BlobsModelFactory.BlobItem(name: blobName);

        _containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItem));

        _containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(new ThrowingBlobClient());

        var helper = new BlobRagHelper(_loggerMock.Object);

        var result = await helper.GetCompanyRagJsonContentsAsync(_containerClientMock.Object, "company_123/CompanyRAGData/");

        Assert.Empty(result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error reading blob")),
                It.Is<Exception>(ex => ex.Message == "Simulated failure"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyRagJsonContentsAsync_ReturnsEmpty_WhenContainerIsNull()
    {
        var helper = new BlobRagHelper(_loggerMock.Object);
        var result = await helper.GetCompanyRagJsonContentsAsync(null!, "some-path");
        Assert.Empty(result);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Blob container is null.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyRagJsonContentsAsync_SkipsNonJsonBlobs()
    {
        var nonJsonBlob = BlobsModelFactory.BlobItem(name: "company_123/file.txt");

        _containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(nonJsonBlob));

        var helper = new BlobRagHelper(_loggerMock.Object);
        var result = await helper.GetCompanyRagJsonContentsAsync(_containerClientMock.Object, "company_123/");

        Assert.Empty(result);
    }

    private static AsyncPageable<BlobItem> MockAsyncPageable(params BlobItem[] items)
    {
        async IAsyncEnumerable<BlobItem> GetAsync()
        {
            foreach (var item in items)
            {
                await Task.Yield(); // simulate asynchronous behavior
                yield return item;
            }
        }

        return new TestAsyncPageable<BlobItem>(GetAsync());
    }

    private class TestAsyncPageable<T> : AsyncPageable<T>
    {
        private readonly IAsyncEnumerable<T> _enumerable;

        public TestAsyncPageable(IAsyncEnumerable<T> enumerable) => _enumerable = enumerable;

        public override async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var item in _enumerable.WithCancellation(cancellationToken))
                yield return item;
        }

        public override IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null) =>
            throw new NotImplementedException();
    }

    private class FakeBlobClient : BlobClient
    {
        private readonly string _jsonContent;

        public FakeBlobClient(string jsonContent) => _jsonContent = jsonContent;

        public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken = default)
        {
            var result = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(_jsonContent));
            return Task.FromResult(Response.FromValue(result, Mock.Of<Response>()));
        }
    }

    private class ThrowingBlobClient : BlobClient
    {
        public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken = default)
        {
            throw new Exception("Simulated failure");
        }
    }

    public class TestOutputTextWriter : TextWriter
    {
        private readonly ITestOutputHelper _output;
        public TestOutputTextWriter(ITestOutputHelper output) => _output = output;
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string? value) => _output.WriteLine(value ?? "");
        public override void Write(char value) => _output.WriteLine(value.ToString());
    }
}