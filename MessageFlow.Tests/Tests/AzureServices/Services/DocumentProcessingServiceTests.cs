using Azure;
using Azure.AI.DocumentIntelligence;
using MessageFlow.AzureServices.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.AzureServices.Services;

public class DocumentProcessingServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<DocumentProcessingService>> _loggerMock = new();

    public DocumentProcessingServiceTests()
    {
        _configMock.Setup(c => c["azure-documentintelligence-endpoint"]).Returns("https://mock.endpoint");
        _configMock.Setup(c => c["azure-documentintelligence-key"]).Returns("mock-key");
    }

    [Fact]
    public async Task ExtractTextFromDocumentAsync_ReturnsText_WhenValidDocument()
    {
        var mockClient = new Mock<DocumentIntelligenceClient>();

        var mockOperation = new Mock<Operation<AnalyzeResult>>();
        mockOperation.Setup(o => o.HasCompleted).Returns(true);

        mockClient
            .Setup(c => c.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-layout",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(mockOperation.Object);

        var service = new TestableDocumentProcessingService(
            content: "Realistic dynamic content",
            config: _configMock.Object,
            logger: _loggerMock.Object,
            client: mockClient.Object
        );

        var result = await service.ExtractTextFromDocumentAsync(new MemoryStream(), "application/pdf");

        Assert.Equal("Realistic dynamic content", result);
    }

    [Fact]
    public async Task ExtractTextFromDocumentAsync_ReturnsEmpty_WhenExceptionThrown()
    {
        // Arrange
        var mockClient = new Mock<DocumentIntelligenceClient>();
        mockClient.Setup(c => c.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-layout",
            It.IsAny<BinaryData>(),
            It.IsAny<CancellationToken>())
        ).ThrowsAsync(new Exception("fail"));

        var service = new TestableDocumentProcessingService(
            content: "should not be used",
            config: _configMock.Object,
            logger: _loggerMock.Object,
            client: mockClient.Object
        );

        // Act
        var result = await service.ExtractTextFromDocumentAsync(new MemoryStream(), "application/pdf");

        // Assert
        Assert.Equal(string.Empty, result);
        _loggerMock.VerifyLogContains(LogLevel.Error, "Error processing document");
    }
}

public class TestableDocumentProcessingService : DocumentProcessingService
{
    private readonly string _simulatedContent;
    private DocumentIntelligenceClient? _mockClient;

    public TestableDocumentProcessingService(
        string content,
        IConfiguration config,
        ILogger<DocumentProcessingService> logger,
        DocumentIntelligenceClient client
    ) : base(config, logger)
    {
        _simulatedContent = content;
        _mockClient = client;
    }

    protected internal override string ExtractTextFromResult(AnalyzeResult result)
    {
        return _simulatedContent;
    }

    protected internal override DocumentIntelligenceClient GetClient() => _mockClient!;
}