using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.CompanyManagement.Queries
{
    public class GetCompanyByIdQueryHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<GetCompanyByIdQueryHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public GetCompanyByIdQueryHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<GetCompanyByIdQueryHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_SuperAdmin_CanAccessAnyCompany()
        {
            var companyId = "c123";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((false, null, true, ""));

            _unitOfWorkMock.Setup(x => x.Companies.GetCompanyWithDetailsByIdAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            var handler = new GetCompanyByIdQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyByIdQuery(companyId), default);

            Assert.NotNull(result);
            Assert.Equal(companyId, result!.Id);
        }

        [Fact]
        public async Task Handle_AdminAccessingOwnCompany_ReturnsCompany()
        {
            var companyId = "c1";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(x => x.Companies.GetCompanyWithDetailsByIdAsync(companyId))
                .ReturnsAsync(new Company { Id = companyId });

            var handler = new GetCompanyByIdQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyByIdQuery(companyId), default);

            Assert.NotNull(result);
            Assert.Equal(companyId, result!.Id);
        }

        [Fact]
        public async Task Handle_AdminAccessingOtherCompany_ReturnsNull()
        {
            var companyId = "unauthorized-id";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((false, "another-company", false, "Access denied"));

            var handler = new GetCompanyByIdQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyByIdQuery(companyId), default);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsNull()
        {
            var companyId = "c404";
            _authHelperMock.Setup(x => x.CompanyAccess(companyId))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(x => x.Companies.GetCompanyWithDetailsByIdAsync(companyId))
                .ReturnsAsync((Company?)null);

            var handler = new GetCompanyByIdQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyByIdQuery(companyId), default);

            Assert.Null(result);
        }
    }
}
