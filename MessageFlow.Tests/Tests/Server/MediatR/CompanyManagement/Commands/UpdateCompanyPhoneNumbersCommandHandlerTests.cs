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
    public class UpdateCompanyPhoneNumbersCommandHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<UpdateCompanyPhoneNumbersCommandHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public UpdateCompanyPhoneNumbersCommandHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<UpdateCompanyPhoneNumbersCommandHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_ValidPhoneNumbers_Success()
        {
            var companyId = "company-1";
            var phoneDTOs = new List<CompanyPhoneNumberDTO>
            {
                new() { Id = "1", PhoneNumber = "1234567890", Description = "Main", CompanyId = companyId }
            };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            _unitOfWorkMock
                .Setup(u => u.CompanyPhoneNumbers.UpdatePhoneNumbersAsync(
                    companyId,
                    It.IsAny<List<CompanyPhoneNumber>>()))
                .Returns(Task.CompletedTask);


            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var handler = new UpdateCompanyPhoneNumbersCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyPhoneNumbersCommand(phoneDTOs), default);

            Assert.True(result.success);
            Assert.Equal("Company phone numbers updated successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_NoPhoneNumbersProvided_ReturnsFalse()
        {
            var handler = new UpdateCompanyPhoneNumbersCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyPhoneNumbersCommand(new List<CompanyPhoneNumberDTO>()), default);

            Assert.False(result.success);
            Assert.Equal("No phone numbers provided for update.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_InvalidCompanyId_ReturnsFalse()
        {
            var handler = new UpdateCompanyPhoneNumbersCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyPhoneNumbersCommand(new List<CompanyPhoneNumberDTO>
            {
                new() { Id = "1", PhoneNumber = "0000", Description = "Invalid", CompanyId = "" }
            }), default);

            Assert.False(result.success);
            Assert.Equal("Invalid CompanyId provided.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var companyId = "unauthorized";

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((false, null, false, "Not allowed"));

            var handler = new UpdateCompanyPhoneNumbersCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyPhoneNumbersCommand(new List<CompanyPhoneNumberDTO>
            {
                new() { Id = "1", PhoneNumber = "1234", Description = "test", CompanyId = companyId }
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

            var handler = new UpdateCompanyPhoneNumbersCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyPhoneNumbersCommand(new List<CompanyPhoneNumberDTO>
            {
                new() { Id = "1", PhoneNumber = "9999", Description = "test", CompanyId = companyId }
            }), default);

            Assert.False(result.success);
            Assert.Equal("Company not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_AdminUpdatingOtherCompany_ReturnsFalse()
        {
            var companyId = "company-1";
            var userCompanyId = "company-other"; // different than target

            var phoneDTOs = new List<CompanyPhoneNumberDTO>
            {
                new() { Id = "1", PhoneNumber = "1234567890", Description = "Support", CompanyId = companyId }
            };

            _authHelperMock.Setup(a => a.CompanyAccess(companyId))
                .ReturnsAsync((true, userCompanyId, false, "")); // Admin, not super, different company

            var handler = new UpdateCompanyPhoneNumbersCommandHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new UpdateCompanyPhoneNumbersCommand(phoneDTOs), default);

            Assert.False(result.success);
            Assert.Equal("Admins can only update their own company's emails.", result.errorMessage);
        }

    }
}
