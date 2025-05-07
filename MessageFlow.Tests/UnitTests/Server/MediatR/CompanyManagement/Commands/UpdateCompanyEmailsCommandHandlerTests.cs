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

namespace MessageFlow.Tests.UnitTests.Server.MediatR.CompanyManagement.Commands
{
    public class UpdateCompanyEmailsCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<UpdateCompanyEmailsCommandHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public UpdateCompanyEmailsCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<UpdateCompanyEmailsCommandHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_ValidEmails_Success()
        {
            var companyId = "company-1";
            var emailDTOs = new List<CompanyEmailDTO>
            {
                new() { Id = "1", EmailAddress = "test@email.com", Description = "Main", CompanyId = companyId }
            };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            _unitOfWorkMock.Setup(u => u.CompanyEmails.UpdateEmailsAsync(companyId, It.IsAny<List<CompanyEmail>>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var handler = new UpdateCompanyEmailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyEmailsCommand(emailDTOs), default);

            Assert.True(result.success);
            Assert.Equal("Company emails updated successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_NoEmailsProvided_ReturnsFalse()
        {
            var handler = new UpdateCompanyEmailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyEmailsCommand(new List<CompanyEmailDTO>()), default);

            Assert.False(result.success);
            Assert.Equal("No emails provided for update.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_InvalidCompanyId_ReturnsFalse()
        {
            var emailDTOs = new List<CompanyEmailDTO>
            {
                new() { Id = "1", EmailAddress = "bad@email.com", Description = "Fail", CompanyId = "" }
            };

            var handler = new UpdateCompanyEmailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyEmailsCommand(emailDTOs), default);

            Assert.False(result.success);
            Assert.Equal("Invalid CompanyId provided.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var companyId = "unauth";

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((false, null, false, "Not allowed"));

            var handler = new UpdateCompanyEmailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyEmailsCommand(new List<CompanyEmailDTO>
            {
                new() { Id = "1", EmailAddress = "x@y.com", Description = "desc", CompanyId = companyId }
            }), default);

            Assert.False(result.success);
            Assert.Equal("Not allowed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsFalse()
        {
            var companyId = "missing";

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync((Company?)null);

            var handler = new UpdateCompanyEmailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyEmailsCommand(new List<CompanyEmailDTO>
            {
                new() { Id = "1", EmailAddress = "x@y.com", Description = "desc", CompanyId = companyId }
            }), default);

            Assert.False(result.success);
            Assert.Equal("Company not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_AdminUpdatingOtherCompany_ReturnsFalse()
        {
            var companyId = "company-123";
            var userCompanyId = "company-789"; // different company (admin's company)

            var emailDTOs = new List<CompanyEmailDTO>
    {
        new() { Id = "1", EmailAddress = "other@company.com", Description = "External", CompanyId = companyId }
    };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, userCompanyId, false, "")); // Simulate Admin of a different company

            var handler = new UpdateCompanyEmailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyEmailsCommand(emailDTOs), default);

            Assert.False(result.success);
            Assert.Equal("Admins can only update their own company's emails.", result.errorMessage);
        }
    }
}
