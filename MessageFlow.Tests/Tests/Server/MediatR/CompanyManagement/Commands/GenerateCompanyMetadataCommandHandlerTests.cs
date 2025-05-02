using AutoMapper;
using MessageFlow.AzureServices.Helpers;
using MessageFlow.AzureServices.Helpers.Interfaces;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using MessageFlow.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.CompanyManagement.Commands
{
    public class GenerateCompanyMetadataCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAzureBlobStorageService> _blobServiceMock;
        private readonly Mock<ILogger<GenerateCompanyMetadataCommandHandler>> _loggerMock;
        private readonly IMapper _mapper;
        private readonly Mock<ICompanyDataHelper> _companyDataHelperMock;

        public GenerateCompanyMetadataCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _blobServiceMock = new Mock<IAzureBlobStorageService>();
            _loggerMock = new Mock<ILogger<GenerateCompanyMetadataCommandHandler>>();
            _companyDataHelperMock = new Mock<ICompanyDataHelper>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_MetadataGeneratedSuccessfully_ReturnsTrue()
        {
            var companyId = "company-1";
            var company = new Company { Id = companyId, CompanyName = "Test", AccountNumber = "123", IndustryType = "IT", WebsiteUrl = "url" };

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetCompanyWithDetailsByIdAsync(companyId))
                .ReturnsAsync(company);

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData
                .GetProcessedFilesByCompanyIdAndTypesAsync(companyId, It.IsAny<List<FileType>>()))
                .ReturnsAsync(new List<ProcessedPretrainData>());

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.RemoveProcessedFiles(It.IsAny<List<ProcessedPretrainData>>()));
            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData.AddProcessedFilesAsync(It.IsAny<List<ProcessedPretrainData>>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            _blobServiceMock.Setup(b => b.UploadFileAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), "application/json", companyId))
                .ReturnsAsync((Stream _, string fileName, string _, string _) => $"https://blob/{fileName}");

            _companyDataHelperMock.Setup(x => x.GenerateStructuredCompanyMetadata(It.IsAny<CompanyDTO>()))
                .Returns((
                    new List<ProcessedPretrainDataDTO> { new() { Id = "file1", CompanyId = companyId } },
                    new List<string> { "{ \"test\": true }" }
                ));

            var handler = new GenerateCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object
            );

            var result = await handler.Handle(new GenerateCompanyMetadataCommand(companyId), default);

            Assert.True(result.success);
            Assert.Equal("Company metadata structured and uploaded successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ReturnsFalse()
        {
            _authHelperMock.Setup(x => x.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((false, null, false, "Not authorized"));

            var handler = new GenerateCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object
            );

            var result = await handler.Handle(new GenerateCompanyMetadataCommand("some-id"), default);

            Assert.False(result.success);
            Assert.Equal("Not authorized", result.errorMessage);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsFalse()
        {
            _authHelperMock.Setup(x => x.CompanyAccess("company-x"))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync("company-x"))
                .ReturnsAsync((Company?)null);

            var handler = new GenerateCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object
            );

            var result = await handler.Handle(new GenerateCompanyMetadataCommand("company-x"), default);

            Assert.False(result.success);
            Assert.Equal("Company not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_FileJsonMismatch_ReturnsFalse()
        {
            var companyId = "company-1";
            var company = new Company
            {
                Id = companyId,
                CompanyName = "Mismatch Co",
                AccountNumber = "999",
                IndustryType = "Tech",
                WebsiteUrl = "site"
            };

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetCompanyWithDetailsByIdAsync(companyId))
                .ReturnsAsync(company);

            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData
                .GetProcessedFilesByCompanyIdAndTypesAsync(companyId, It.IsAny<List<FileType>>()))
                .ReturnsAsync(new List<ProcessedPretrainData>());

            // Override GenerateStructuredCompanyMetadata result
            _companyDataHelperMock.Setup(x => x.GenerateStructuredCompanyMetadata(It.IsAny<CompanyDTO>()))
                .Returns((new List<ProcessedPretrainDataDTO> { new() { Id = "file1", CompanyId = companyId } },
                          new List<string>())); // No matching JSONs


            var handler = new GenerateCompanyMetadataCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _blobServiceMock.Object,
                _loggerMock.Object,
                _companyDataHelperMock.Object
            );

            var result = await handler.Handle(new GenerateCompanyMetadataCommand(companyId), default);

            Assert.False(result.success);
            Assert.Equal("Mismatch between processed metadata files and JSON contents.", result.errorMessage);

        }

    }
}
