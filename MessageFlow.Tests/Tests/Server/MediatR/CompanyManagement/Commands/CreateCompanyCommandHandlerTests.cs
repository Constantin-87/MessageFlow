using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.CompanyManagement.Commands
{
    public class CreateCompanyCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<CreateCompanyCommandHandler>> _loggerMock;

        public CreateCompanyCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CreateCompanyCommandHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_SuperAdminAuthorized_ReturnsSuccess()
        {
            // Arrange
            _authHelperMock.Setup(x => x.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.AddEntityAsync(It.IsAny<Company>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);


            var handler = new CreateCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object);

            var dto = new CompanyDTO
            {
                CompanyName = "Test Company",
                AccountNumber = "ACC-001",
                Description = "Desc",
                IndustryType = "Tech",
                WebsiteUrl = "https://example.com"
            };

            var command = new CreateCompanyCommand(dto);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Company created successfully", result.errorMessage);
            _unitOfWorkMock.Verify(u => u.Companies.AddEntityAsync(It.IsAny<Company>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task Handle_NotSuperAdmin_ReturnsUnauthorized()
        {
            // Arrange
            _authHelperMock.Setup(x => x.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((true, null, false, ""));

            var handler = new CreateCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object);

            var dto = new CompanyDTO
            {
                CompanyName = "Unauthorized Co",
                AccountNumber = "ACC-002",
                Description = "Nope",
                IndustryType = "Other",
                WebsiteUrl = "https://unauthorized.com"
            };

            var command = new CreateCompanyCommand(dto);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.False(result.success);
            Assert.Equal("Only SuperAdmins can create companies.", result.errorMessage);
            _unitOfWorkMock.Verify(u => u.Companies.AddEntityAsync(It.IsAny<Company>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}
