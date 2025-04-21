using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.CompanyManagement.Queries
{
    public class GetAllCompaniesQueryHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetAllCompaniesQueryHandler>> _loggerMock;

        public GetAllCompaniesQueryHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetAllCompaniesQueryHandler>>();
        }

        [Fact]
        public async Task Handle_SuperAdmin_ReturnsAllCompanies()
        {
            var companies = new List<Company> { new() { Id = "c1" }, new() { Id = "c2" } };
            var companyDTOs = companies.Select(c => new CompanyDTO { Id = c.Id }).ToList();

            _authHelperMock.Setup(a => a.CompanyAccess(""))
                .ReturnsAsync((true, null, true, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetAllCompaniesWithUserCountAsync())
                .ReturnsAsync(companies);

            _mapperMock.Setup(m => m.Map<List<CompanyDTO>>(companies)).Returns(companyDTOs);

            var handler = new GetAllCompaniesQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetAllCompaniesQuery(), default);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Handle_Admin_ReturnsOwnCompany()
        {
            var company = new Company { Id = "c1" };
            var companyDTO = new CompanyDTO { Id = "c1" };

            _authHelperMock.Setup(a => a.CompanyAccess(""))
                .ReturnsAsync((true, "c1", false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync("c1"))
                .ReturnsAsync(company);

            _mapperMock.Setup(m => m.Map<CompanyDTO>(company)).Returns(companyDTO);

            var handler = new GetAllCompaniesQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetAllCompaniesQuery(), default);

            Assert.Single(result);
            Assert.Equal("c1", result.First().Id);
        }

        [Fact]
        public async Task Handle_Admin_CompanyIdMissing_ReturnsEmptyList()
        {
            _authHelperMock.Setup(a => a.CompanyAccess(""))
                .ReturnsAsync((true, null, false, ""));

            var handler = new GetAllCompaniesQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetAllCompaniesQuery(), default);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_Admin_CompanyNotFound_Throws()
        {
            _authHelperMock.Setup(a => a.CompanyAccess(""))
                .ReturnsAsync((true, "c999", false, ""));

            _unitOfWorkMock.Setup(u => u.Companies.GetByIdStringAsync("c999"))
                .ReturnsAsync((Company?)null);

            var handler = new GetAllCompaniesQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new GetAllCompaniesQuery(), default));
        }
    }

}
