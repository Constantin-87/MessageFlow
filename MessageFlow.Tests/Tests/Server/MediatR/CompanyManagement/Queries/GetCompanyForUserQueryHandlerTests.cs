using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.CompanyManagement.Queries
{
    public class GetCompanyForUserQueryHandlerTests
    {
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<GetCompanyForUserQueryHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public GetCompanyForUserQueryHandlerTests()
        {
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<GetCompanyForUserQueryHandler>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_UserHasCompany_ReturnsCompany()
        {
            var companyId = "comp123";
            var company = new Company { Id = companyId, CompanyName = "TestCo" };

            _authHelperMock.Setup(x => x.CompanyAccess(string.Empty))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(x => x.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync(company);

            var handler = new GetCompanyForUserQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyForUserQuery(), default);

            Assert.NotNull(result);
            Assert.Equal("TestCo", result.CompanyName);
        }

        [Fact]
        public async Task Handle_NoCompanyIdInClaims_ReturnsEmptyDTO()
        {
            _authHelperMock.Setup(x => x.CompanyAccess(string.Empty))
                .ReturnsAsync((true, null, false, ""));

            var handler = new GetCompanyForUserQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyForUserQuery(), default);

            Assert.NotNull(result);
            Assert.Null(result.Id);
        }

        [Fact]
        public async Task Handle_CompanyNotFound_ReturnsNull()
        {
            var companyId = "missing";

            _authHelperMock.Setup(x => x.CompanyAccess(string.Empty))
                .ReturnsAsync((true, companyId, false, ""));

            _unitOfWorkMock.Setup(x => x.Companies.GetByIdStringAsync(companyId))
                .ReturnsAsync((Company?)null);

            var handler = new GetCompanyForUserQueryHandler(
                _authHelperMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object
            );

            var result = await handler.Handle(new GetCompanyForUserQuery(), default);

            Assert.Null(result);
        }
    }
}
