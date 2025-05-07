using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using MessageFlow.AzureServices.Interfaces;
using System.Text;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.AzureServices.Helpers.Interfaces;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.CompanyManagement.Commands
{
    public class UploadCompanyFilesCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDocumentProcessingService> _docProcessingMock;
        private readonly Mock<IAzureBlobStorageService> _blobServiceMock;
        private readonly Mock<ILogger<UploadCompanyFilesCommandHandler>> _loggerMock;
        private readonly Mock<ICompanyDataHelper> _companyDataHelperMock;
        private readonly IMapper _mapper;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;

        public UploadCompanyFilesCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _docProcessingMock = new Mock<IDocumentProcessingService>();
            _blobServiceMock = new Mock<IAzureBlobStorageService>();
            _loggerMock = new Mock<ILogger<UploadCompanyFilesCommandHandler>>();
            _companyDataHelperMock = new Mock<ICompanyDataHelper>();
            _authHelperMock = new Mock<IAuthorizationHelper>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_ValidUpload_ReturnsSuccess()
        {
            var companyId = "c1";
            var fileDto = new PretrainDataFileDTO
            {
                Id = "f1",
                CompanyId = companyId,
                FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test content"))
            };


            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            var processed = new List<ProcessedPretrainDataDTO> {
                new() { Id = "p1", FileUrl = null, CompanyId = companyId }
            };
            var contents = new List<string> { "{ \"json\": true }" };
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, true, ""));

            _companyDataHelperMock
                .Setup(h => h.ProcessUploadedFilesAsync(It.IsAny<List<PretrainDataFileDTO>>(), _docProcessingMock.Object))
                .ReturnsAsync((processed, contents));

            _blobServiceMock.Setup(b => b.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "application/json", companyId))
                .ReturnsAsync("https://blob/uploaded.json");

            _unitOfWorkMock
            .Setup(u => u.ProcessedPretrainData.AddProcessedFilesAsync(It.IsAny<List<ProcessedPretrainData>>()))
            .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var handler = new UploadCompanyFilesCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                _docProcessingMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new UploadCompanyFilesCommand(new List<PretrainDataFileDTO> { fileDto }), default);

            Assert.True(result.success);
            Assert.Equal("Files uploaded successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsFalse()
        {

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync("x")).ReturnsAsync((Company?)null);

            var handler = new UploadCompanyFilesCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                _docProcessingMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new UploadCompanyFilesCommand(new List<PretrainDataFileDTO>
                {
                    new()
                    {
                        Id = "1",
                        CompanyId = "x",
                        FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test content"))
                    }
                }), default);


            Assert.False(result.success);
            Assert.Equal("Company not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_MismatchedCounts_ReturnsFalse()
        {
            var companyId = "c1";

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            _companyDataHelperMock.Setup(h => h.ProcessUploadedFilesAsync(It.IsAny<List<PretrainDataFileDTO>>(), _docProcessingMock.Object))
                .ReturnsAsync((new List<ProcessedPretrainDataDTO> { new() { Id = "x" } }, new List<string>())); // mismatch

            var handler = new UploadCompanyFilesCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                _docProcessingMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new UploadCompanyFilesCommand(new List<PretrainDataFileDTO>
            {
                new()
                {
                    Id = "1",
                    CompanyId = companyId,
                    FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test content"))
                }
            }), default);


            Assert.False(result.success);
            Assert.Equal("Mismatch between processed files and JSON contents.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_ExceptionDuringUpload_ReturnsFalse()
        {
            var companyId = "err";

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ThrowsAsync(new Exception("boom"));

            var handler = new UploadCompanyFilesCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                _docProcessingMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new UploadCompanyFilesCommand(new List<PretrainDataFileDTO>
            {
                new()
                {
                    Id = "1",
                    CompanyId = companyId,
                    FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test content"))
                }
            }), default);

            Assert.False(result.success);
            Assert.Equal("An error occurred during file upload.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedUpload_ReturnsFalse()
        {
            var companyId = "unauth";
            var fileDto = new PretrainDataFileDTO
            {
                Id = "f1",
                CompanyId = companyId,
                FileContent = new MemoryStream(Encoding.UTF8.GetBytes("fake"))
            };

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((false, null, false, "Access denied"));

            var handler = new UploadCompanyFilesCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                _docProcessingMock.Object,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new UploadCompanyFilesCommand(new List<PretrainDataFileDTO> { fileDto }), default);

            Assert.False(result.success);
            Assert.Equal("Access denied", result.errorMessage);
        }
    }
}
