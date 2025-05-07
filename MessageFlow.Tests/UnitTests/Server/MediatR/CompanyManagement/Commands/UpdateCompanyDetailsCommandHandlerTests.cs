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
    public class UpdateCompanyDetailsCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<UpdateCompanyDetailsCommandHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public UpdateCompanyDetailsCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<UpdateCompanyDetailsCommandHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_SuperAdmin_UpdatesAllFields()
        {
            var companyId = "123";
            var dto = new CompanyDTO
            {
                Id = companyId,
                CompanyName = "Updated Co",
                Description = "Updated Description",
                IndustryType = "Updated Industry",
                WebsiteUrl = "https://updated.com"
            };

            var existing = new Company { Id = companyId };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(existing);

            _unitOfWorkMock.Setup(u => u.Companies.UpdateEntityAsync(existing))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var handler = new UpdateCompanyDetailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyDetailsCommand(dto), default);

            Assert.True(result.success);
            Assert.Equal("Company details updated successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Admin_UpdatesLimitedFields()
        {
            var companyId = "456";
            var dto = new CompanyDTO
            {
                Id = companyId,
                CompanyName = "Admin Co",
                Description = "Admin Desc",
                IndustryType = "Admin IT",
                WebsiteUrl = "https://admin.com"
            };

            var existing = new Company { Id = companyId };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, "")); // Not SuperAdmin

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(existing);

            _unitOfWorkMock.Setup(u => u.Companies.UpdateEntityAsync(existing))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var handler = new UpdateCompanyDetailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyDetailsCommand(dto), default);

            Assert.True(result.success);
            Assert.Equal("Company details updated successfully.", result.errorMessage);

            Assert.Equal("Admin Co", existing.CompanyName);
            Assert.Equal("Admin Desc", existing.Description);
            Assert.Equal("Admin IT", existing.IndustryType);
            Assert.Equal("https://admin.com", existing.WebsiteUrl);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsFalse()
        {
            _authHelperMock.Setup(a => a.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((false, null, false, "Unauthorized"));

            var handler = new UpdateCompanyDetailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyDetailsCommand(new CompanyDTO { Id = "bad-id" }), default);

            Assert.False(result.success);
            Assert.Equal("Unauthorized", result.errorMessage);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsFalse()
        {
            var companyId = "missing";

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, null, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync((Company?)null);

            var handler = new UpdateCompanyDetailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyDetailsCommand(new CompanyDTO { Id = companyId }), default);

            Assert.False(result.success);
            Assert.Equal("Company not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_InvalidCompanyId_ReturnsFalse()
        {
            var handler = new UpdateCompanyDetailsCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyDetailsCommand(new CompanyDTO { Id = "" }), default);

            Assert.False(result.success);
            Assert.Equal("Invalid CompanyId provided.", result.errorMessage);
        }
    }
}
