using MessageFlow.AzureServices.Interfaces;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.CompanyManagement.Commands
{
    public class DeleteCompanyFileCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAzureBlobStorageService> _blobServiceMock;
        private readonly Mock<ILogger<DeleteCompanyFileCommandHandler>> _loggerMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;

        public DeleteCompanyFileCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _blobServiceMock = new Mock<IAzureBlobStorageService>();
            _loggerMock = new Mock<ILogger<DeleteCompanyFileCommandHandler>>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
        }

        [Fact]
        public async Task Handle_FileExists_DeletesSuccessfully()
        {
            var dto = new ProcessedPretrainDataDTO
            {
                Id = "file-1",
                FileUrl = "https://blob/test.txt",
                CompanyId = "company-1"
            };

            var entity = new ProcessedPretrainData
            {
                Id = dto.Id,
                FileUrl = dto.FileUrl,
                CompanyId = dto.CompanyId
            };

            _authHelperMock.Setup(x => x.CompanyAccess(dto.CompanyId))
                .ReturnsAsync((true, dto.CompanyId, true, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetByIdStringAsync(dto.Id))
                .ReturnsAsync(entity);

            _blobServiceMock.Setup(b => b.DeleteFileAsync(dto.FileUrl))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.RemoveEntityAsync(entity))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var handler = new DeleteCompanyFileCommandHandler(
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyFileCommand(dto), default);

            Assert.True(result);
        }

        [Fact]
        public async Task Handle_FileNotFound_ReturnsFalse()
        {
            var dto = new ProcessedPretrainDataDTO
            {
                Id = "missing",
                FileUrl = "url",
                CompanyId = "company-1"
            };

            _authHelperMock.Setup(x => x.CompanyAccess(dto.CompanyId))
                .ReturnsAsync((true, dto.CompanyId, true, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetByIdStringAsync(dto.Id))
                .ReturnsAsync((ProcessedPretrainData?)null);

            var handler = new DeleteCompanyFileCommandHandler(
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyFileCommand(dto), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_BlobDeletionFails_ReturnsFalse()
        {
            var dto = new ProcessedPretrainDataDTO
            {
                Id = "file-2",
                FileUrl = "bad-url",
                CompanyId = "company-1"
            };

            var entity = new ProcessedPretrainData
            {
                Id = dto.Id,
                FileUrl = dto.FileUrl,
                CompanyId = dto.CompanyId
            };

            _authHelperMock.Setup(x => x.CompanyAccess(dto.CompanyId))
                .ReturnsAsync((true, dto.CompanyId, true, ""));

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.GetByIdStringAsync(dto.Id))
                .ReturnsAsync(entity);

            _blobServiceMock.Setup(b => b.DeleteFileAsync(dto.FileUrl))
                .ReturnsAsync(false);

            var handler = new DeleteCompanyFileCommandHandler(
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyFileCommand(dto), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_UnauthorizedAdmin_ReturnsFalse()
        {
            var dto = new ProcessedPretrainDataDTO
            {
                Id = "f3",
                FileUrl = "url",
                CompanyId = "company-x"
            };

            _authHelperMock.Setup(x => x.CompanyAccess(dto.CompanyId))
                .ReturnsAsync((false, null, false, "Unauthorized"));

            var handler = new DeleteCompanyFileCommandHandler(
                _unitOfWorkMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyFileCommand(dto), default);

            Assert.False(result);
        }
    }
}
