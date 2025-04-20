using AutoMapper;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.CompanyManagement.Commands
{
    public class CreateSearchIndexCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAzureSearchService> _searchServiceMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<CreateSearchIndexCommandHandler>> _loggerMock;

        public CreateSearchIndexCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _searchServiceMock = new Mock<IAzureSearchService>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<CreateSearchIndexCommandHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_SuperAdmin_CreatesIndex()
        {
            var companyId = "company1";
            var processedFiles = new List<ProcessedPretrainData>
            {
                new() { Id = "1", CompanyId = companyId, FileUrl = "url", FileType = Shared.Enums.FileType.Other }
            };

            _unitOfWorkMock.Setup(x => x.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(processedFiles);

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, true, ""));

            var handler = new CreateSearchIndexCommandHandler(
                _unitOfWorkMock.Object,
                _searchServiceMock.Object,
                _mapper,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateSearchIndexCommand(companyId), default);

            Assert.True(result.success);
            Assert.Equal("Index created and populated successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_NoProcessedFiles_ReturnsFalse()
        {
            var companyId = "empty";

            _unitOfWorkMock.Setup(x => x.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(new List<ProcessedPretrainData>());

            var handler = new CreateSearchIndexCommandHandler(
                _unitOfWorkMock.Object,
                _searchServiceMock.Object,
                _mapper,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateSearchIndexCommand(companyId), default);

            Assert.False(result.success);
            Assert.Equal("No processed data found for this company.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedAdmin_ReturnsError()
        {
            var companyId = "unauth-co";
            var processedFiles = new List<ProcessedPretrainData>
            {
                new() { Id = "1", CompanyId = companyId, FileUrl = "url" }
            };

            _unitOfWorkMock.Setup(x => x.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(processedFiles);

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((false, null, false, "Access denied"));

            var handler = new CreateSearchIndexCommandHandler(
                _unitOfWorkMock.Object,
                _searchServiceMock.Object,
                _mapper,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateSearchIndexCommand(companyId), default);

            Assert.False(result.success);
            Assert.Equal("Access denied", result.errorMessage);
        }

        [Fact]
        public async Task Handle_AdminForOwnCompany_CreatesIndex()
        {
            var companyId = "admin-co";
            var processedFiles = new List<ProcessedPretrainData>
            {
                new() { Id = "1", CompanyId = companyId, FileUrl = "url" }
            };

            _unitOfWorkMock.Setup(x => x.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(processedFiles);

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, false, ""));

            var handler = new CreateSearchIndexCommandHandler(
                _unitOfWorkMock.Object,
                _searchServiceMock.Object,
                _mapper,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateSearchIndexCommand(companyId), default);

            Assert.True(result.success);
            Assert.Equal("Index created and populated successfully.", result.errorMessage);
        }
    }
}
