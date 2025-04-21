using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.CompanyManagement.Commands
{
    public class DeleteCompanyMetadataCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAzureBlobStorageService> _blobServiceMock;
        private readonly Mock<ILogger<DeleteCompanyMetadataCommandHandler>> _loggerMock;

        public DeleteCompanyMetadataCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _blobServiceMock = new Mock<IAzureBlobStorageService>();
            _loggerMock = new Mock<ILogger<DeleteCompanyMetadataCommandHandler>>();
        }

        [Fact]
        public async Task Handle_AllFilesDeleted_Success()
        {
            var companyId = "company-1";

            var files = new List<ProcessedPretrainData>
            {
                new() { Id = "f1", FileUrl = "url1" },
                new() { Id = "f2", FileUrl = "url2" }
            };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(files);

            _blobServiceMock.Setup(b => b.DeleteFileAsync(It.IsAny<string>())).ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.RemoveProcessedFiles(files));
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var handler = new DeleteCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyMetadataCommand(companyId), default);

            Assert.True(result.success);
            Assert.Equal("All company metadata files deleted successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsFalse()
        {
            _authHelperMock.Setup(a => a.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((false, null, false, "Not allowed"));

            var handler = new DeleteCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyMetadataCommand("company-x"), default);

            Assert.False(result.success);
            Assert.Equal("Not allowed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_NoFiles_ReturnsFalse()
        {
            var companyId = "company-empty";

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(new List<ProcessedPretrainData>());

            var handler = new DeleteCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyMetadataCommand(companyId), default);

            Assert.False(result.success);
            Assert.Equal("No metadata files found for this company.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_SomeFilesFailDeletion_ReturnsPartialFailure()
        {
            var companyId = "company-partial";

            var files = new List<ProcessedPretrainData>
            {
                new() { Id = "f1", FileUrl = "url1" },
                new() { Id = "f2", FileUrl = "url2" }
            };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(files);

            _blobServiceMock.Setup(b => b.DeleteFileAsync("url1")).ReturnsAsync(true);
            _blobServiceMock.Setup(b => b.DeleteFileAsync("url2")).ReturnsAsync(false);

            var handler = new DeleteCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyMetadataCommand(companyId), default);

            Assert.False(result.success);
            Assert.Equal("Some files failed to delete from Azure Blob Storage, their database records were retained.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnexpectedException_ReturnsError()
        {
            var companyId = "company-crash";

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ThrowsAsync(new Exception("Unexpected DB error"));

            var handler = new DeleteCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyMetadataCommand(companyId), default);

            Assert.False(result.success);
            Assert.Equal("An error occurred while deleting metadata.", result.errorMessage);
        }

    }
}
