using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Server.MediatR.TeamManagement.Commands;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using MessageFlow.Shared.DTOs;
using MessageFlow.Shared.Enums;
using MessageFlow.DataAccess.Repositories;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.CompanyManagement.Commands
{
    public class DeleteCompanyCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<DeleteCompanyCommandHandler>> _loggerMock;
        private readonly Mock<IMediator> _mediatorMock;

        public DeleteCompanyCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<DeleteCompanyCommandHandler>>();
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task Handle_SuperAdmin_CanDeleteCompany()
        {
            // Arrange
            var companyId = "company123";
            var company = new Company { Id = companyId, CompanyName = "Test Co" };

            // Mock the authorization helper to return SuperAdmin access
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, null, true, ""));  // SuperAdmin authorized

            // Mock the unitOfWork to return a valid company
            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(company);  // Return the mocked company object

            // Mock the removal of the company and saving changes
            _unitOfWorkMock.Setup(u => u.Companies.RemoveEntityAsync(company))
                .Returns(Task.CompletedTask);  // Simulate removing the company

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);  // Simulate saving changes

            // Mock the ProcessedPretrainData to return some metadata files
            var mockMetadataFiles = new List<ProcessedPretrainData>
    {
        new ProcessedPretrainData
        {
            Id = "file1",
            FileDescription = "Description of file 1",
            FileUrl = "http://example.com/file1",
            CompanyId = "company123",
            FileType = FileType.FAQFile,
            ProcessedAt = DateTime.Now
        }
    };

            // Mock the ProcessedPretrainDataRepository to return the mock data
            var mockProcessedPretrainDataRepo = new Mock<IProcessedPretrainDataRepository>();
            mockProcessedPretrainDataRepo.Setup(x => x.GetProcessedFilesByCompanyIdAsync(companyId))
                .ReturnsAsync(mockMetadataFiles);  // Return the mock metadata files

            // Set up the unitOfWork to return the mocked ProcessedPretrainDataRepository
            _unitOfWorkMock.Setup(u => u.ProcessedPretrainData)
                .Returns(mockProcessedPretrainDataRepo.Object);

            // Mock mediator commands
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUsersByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);  // Simulate successful user deletion

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamsByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);  // Simulate successful team deletion

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteCompanyMetadataCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, ""));  // Simulate successful metadata deletion

            // Create the handler
            var handler = new DeleteCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );

            var command = new DeleteCompanyCommand(companyId);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            Assert.True(result.success);  // The result should be successful now
            Assert.Equal("Company and all associated data deleted successfully.", result.errorMessage);  // Assert success message
        }






        [Fact]
        public async Task Handle_NonSuperAdmin_ReturnsUnauthorized()
        {
            _authHelperMock.Setup(x => x.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((true, null, false, ""));

            var handler = new DeleteCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );

            var command = new DeleteCompanyCommand("id");

            var result = await handler.Handle(command, default);

            Assert.False(result.success);
            Assert.Equal("Only SuperAdmins can delete companies.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsError()
        {
            var companyId = "notfound";

            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync((Company?)null);

            var handler = new DeleteCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );

            var command = new DeleteCompanyCommand(companyId);
            var result = await handler.Handle(command, default);

            Assert.False(result.success);
            Assert.Equal("Company not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UserDeletionFails_ReturnsError()
        {
            var company = new Company { Id = "id" };

            _authHelperMock.Setup(x => x.CompanyAccess(company.Id))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(company.Id))
                .ReturnsAsync(company);

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUsersByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Fail

            var handler = new DeleteCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );

            var command = new DeleteCompanyCommand(company.Id);
            var result = await handler.Handle(command, default);

            Assert.False(result.success);
            Assert.Equal("Failed to delete users for this company.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_TeamDeletionFails_ReturnsError()
        {
            var company = new Company { Id = "id" };

            _authHelperMock.Setup(x => x.CompanyAccess(company.Id))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(company.Id))
                .ReturnsAsync(company);

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUsersByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamsByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Simulate failure

            var handler = new DeleteCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyCommand(company.Id), default);

            Assert.False(result.success);
            Assert.Equal("Failed to delete teams for this company.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_MetadataDeletionFails_ReturnsError()
        {
            var company = new Company { Id = "id" };

            _authHelperMock.Setup(x => x.CompanyAccess(company.Id))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(company.Id))
                .ReturnsAsync(company);

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUsersByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamsByCompanyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteCompanyMetadataCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, "Metadata error"));

            var handler = new DeleteCompanyCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mediatorMock.Object
            );

            var result = await handler.Handle(new DeleteCompanyCommand(company.Id), default);

            Assert.False(result.success);
            Assert.Equal("Failed to delete metadata for this company.", result.errorMessage);
        }
    }
}
