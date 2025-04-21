
//using System.Text;
//using Azure;
//using Azure.Storage.Blobs;
//using Azure.Storage.Blobs.Models;
//using MessageFlow.AzureServices.Services;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit.Abstractions;

//namespace MessageFlow.Tests.Tests.AzureServices.Services;

//public class AzureBlobStorageServiceTests
//{
//    private readonly Mock<BlobServiceClient> _blobServiceClientMock = new();
//    private readonly Mock<BlobContainerClient> _containerClientMock = new();
//    private readonly Mock<BlobClient> _blobClientMock = new();
//    private readonly Mock<ILogger<AzureBlobStorageService>> _loggerMock = new();
//    private readonly IConfiguration _config;

//    private readonly ITestOutputHelper _output;

//    public AzureBlobStorageServiceTests(ITestOutputHelper output)
//    {
//        _output = output;
//        Console.SetOut(new TestOutputTextWriter(output));

//        var configMock = new Mock<IConfiguration>();
//        configMock.Setup(c => c["azure-storage-account-conn-string"]).Returns("UseDevelopmentStorage=true");
//        _config = configMock.Object;

//        _blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
//            .Returns(_containerClientMock.Object);

//        _containerClientMock.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, default))
//            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

//        _containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
//            .Returns(_blobClientMock.Object);

//        _blobClientMock
//            .Setup(c => c.UploadAsync(
//                It.IsAny<Stream>(),
//                It.IsAny<BlobUploadOptions>(),
//                It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());


//        _blobClientMock.Setup(c => c.Uri).Returns(new Uri("https://mockstorage.blob.core.windows.net/company_123/test.json"));
//        _output = output;
//    }


//    /// <summary>
//    /// Helper classes
//    /// </summary>
//    private class TestOutputTextWriter : TextWriter
//    {
//        private readonly ITestOutputHelper _output;

//        public TestOutputTextWriter(ITestOutputHelper output)
//        {
//            _output = output;
//        }

//        public override void WriteLine(string? value)
//        {
//            _output.WriteLine(value ?? "");
//        }

//        public override void Write(char value)
//        {
//            _output.WriteLine(value.ToString());
//        }

//        public override Encoding Encoding => Encoding.UTF8;
//    }

//    [Fact]
//    public async Task UploadFileAsync_ReturnsBlobUrl()
//    {
//        var service = new AzureBlobStorageService(_config, _loggerMock.Object, _blobServiceClientMock.Object);
//        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
//        var result = await service.UploadFileAsync(stream, "test.json", "application/json", "123");

//        Assert.Contains("company_123", result);
//        Assert.Contains("test.json", result);
//    }

//    [Fact]
//    public async Task DeleteFileAsync_ReturnsTrue_WhenSuccessful()
//    {
//        _blobClientMock.Setup(x => x.DeleteIfExistsAsync(
//            It.IsAny<DeleteSnapshotsOption>(),
//            It.IsAny<BlobRequestConditions>(),
//            It.IsAny<CancellationToken>()))
//        .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

//        var service = new AzureBlobStorageService(_config, _loggerMock.Object, _blobServiceClientMock.Object);
//        var result = await service.DeleteFileAsync("https://mockstorage.blob.core.windows.net/company-files/company_123/file.txt");

//        Assert.True(result);
//    }

//    [Fact]
//    public async Task DeleteFileAsync_ReturnsFalse_OnException()
//    {
//        _blobClientMock
//            .Setup(x => x.DeleteIfExistsAsync(
//                It.IsAny<DeleteSnapshotsOption>(),
//                It.IsAny<BlobRequestConditions>(),
//                It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new Exception("Delete failed"));

//        var service = new AzureBlobStorageService(_config, _loggerMock.Object, _blobServiceClientMock.Object);
//        var result = await service.DeleteFileAsync("https://mockstorage.blob.core.windows.net/company-files/company_123/file.txt");

//        Assert.False(result);
//    }

//    [Fact]
//    public async Task DownloadFileContentAsync_ReturnsContent_WhenBlobExists()
//    {
//        _blobClientMock.Setup(x => x.ExistsAsync(default))
//            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

//        var content = BinaryData.FromString("hello");
//        var downloadResult = BlobsModelFactory.BlobDownloadResult(
//            content: content,
//            details: BlobsModelFactory.BlobDownloadDetails()
//        );

//        var responseMock = new Mock<Response<BlobDownloadResult>>();
//        responseMock.Setup(r => r.Value).Returns(downloadResult);

//        _blobClientMock
//            .Setup(x => x.DownloadContentAsync(default))
//            .ReturnsAsync(responseMock.Object);

//        var service = new AzureBlobStorageService(_config, _loggerMock.Object, _blobServiceClientMock.Object);
//        var result = await service.DownloadFileContentAsync("https://mock.blob.core.windows.net/company-files/company_123/file.json");

//        Assert.Equal("hello", result);
//    }



//    [Fact]
//    public async Task DownloadFileContentAsync_ReturnsEmpty_WhenBlobNotFound()
//    {
//        _blobClientMock.Setup(x => x.ExistsAsync(default)).ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

//        var service = new AzureBlobStorageService(_config, _loggerMock.Object, _blobServiceClientMock.Object);
//        var result = await service.DownloadFileContentAsync("https://mock.blob.core.windows.net/company-files/company_123/missing.json");

//        Assert.Equal(string.Empty, result);
//    }

//    [Fact]
//    public async Task DownloadFileAsStreamAsync_Throws_WhenBlobNotFound()
//    {
//        _blobClientMock.Setup(x => x.ExistsAsync(default)).ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

//        var service = new AzureBlobStorageService(_config, _loggerMock.Object, _blobServiceClientMock.Object);

//        await Assert.ThrowsAsync<FileNotFoundException>(() =>
//            service.DownloadFileAsStreamAsync("https://mock.blob.core.windows.net/company-files/company_123/missing.json"));
//    }
//}
