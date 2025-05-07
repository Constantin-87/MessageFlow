using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.CompanyManagement.Queries
{
    public class GetCompanyMetadataQueryHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IAzureBlobStorageService> _blobServiceMock;
        private readonly Mock<ILogger<GetCompanyMetadataQueryHandler>> _loggerMock;

        public GetCompanyMetadataQueryHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _blobServiceMock = new Mock<IAzureBlobStorageService>();
            _loggerMock = new Mock<ILogger<GetCompanyMetadataQueryHandler>>();
        }

        [Fact]
        public async Task Handle_AuthorizedWithMetadata_ReturnsSuccess()
        {
            var companyId = "test-company";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));
            _blobServiceMock.Setup(x => x.GetAllCompanyRagDataFilesAsync(companyId))
                .ReturnsAsync("some-metadata");

            var handler = new GetCompanyMetadataQueryHandler(
                _authHelperMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetCompanyMetadataQuery(companyId), default);

            Assert.True(result.success);
            Assert.Equal("some-metadata", result.metadata);
            Assert.Equal("Metadata retrieved successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsError()
        {
            var companyId = "unauthorized";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((false, null, false, "Not allowed"));

            var handler = new GetCompanyMetadataQueryHandler(
                _authHelperMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetCompanyMetadataQuery(companyId), default);

            Assert.False(result.success);
            Assert.Equal(string.Empty, result.metadata);
            Assert.Equal("Not allowed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_MetadataNotFound_ReturnsError()
        {
            var companyId = "no-metadata";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));
            _blobServiceMock.Setup(x => x.GetAllCompanyRagDataFilesAsync(companyId))
                .ReturnsAsync(string.Empty);

            var handler = new GetCompanyMetadataQueryHandler(
                _authHelperMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetCompanyMetadataQuery(companyId), default);

            Assert.False(result.success);
            Assert.Equal("Metadata not found.", result.errorMessage);
        }
    }
}
