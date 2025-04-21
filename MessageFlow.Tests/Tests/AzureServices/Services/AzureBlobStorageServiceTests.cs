//using Azure;
//using Azure.Storage.Blobs;
//using Azure.Storage.Blobs.Models;
//using MessageFlow.AzureServices.Services;
//using Microsoft.Extensions.Configuration;
//using Moq;
//using System.Text;

//namespace MessageFlow.Tests.Tests.AzureServices.Services;
//public class AzureBlobStorageServiceTests
//{
//    private readonly Mock<BlobServiceClient> _blobServiceClientMock = new();
//    private readonly Mock<BlobContainerClient> _containerClientMock = new();
//    private readonly Mock<BlobClient> _blobClientMock = new();
//    private readonly IConfiguration _config;

//    public AzureBlobStorageServiceTests()
//    {
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
//    .Setup(c => c.UploadAsync(
//        It.IsAny<Stream>(),
//        It.IsAny<BlobUploadOptions>(),
//        It.IsAny<CancellationToken>()))
//    .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());


//    }

//    [Fact]
//    public async Task UploadFileAsync_ReturnsBlobUrl()
//    {
//        // Arrange
//        var service = new AzureBlobStorageService(_config);
//        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
//        var fileName = "test.json";
//        var contentType = "application/json";
//        var companyId = "123";

//        // Act
//        var result = await service.UploadFileAsync(stream, fileName, contentType, companyId);

//        // Assert
//        Assert.Contains("company_123", result);
//        Assert.Contains(fileName, result);
//    }

//}
