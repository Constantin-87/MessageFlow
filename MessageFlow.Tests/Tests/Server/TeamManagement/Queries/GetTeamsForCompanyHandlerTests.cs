using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.QueryHandlers;
using MessageFlow.Server.MediatorComponents.TeamManagement.Queries;
using MessageFlow.Infrastructure.Mappings;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.TeamManagement.Queries
{
    public class GetTeamsForCompanyHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<GetTeamsForCompanyHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public GetTeamsForCompanyHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<GetTeamsForCompanyHandler>>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
        }

        [Fact]
        public async Task Handle_AuthorizedUser_ReturnsTeams()
        {
            var companyId = "company1";
            var teams = new List<Team> { new() { Id = "t1", TeamName = "Support", CompanyId = companyId } };

            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamsByCompanyIdAsync(companyId)).ReturnsAsync(teams);

            var handler = new GetTeamsForCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _mapper,
                _loggerMock.Object);

            var result = await handler.Handle(new GetTeamsForCompanyQuery(companyId), default);

            Assert.Single(result);
            Assert.Equal("Support", result.First().TeamName);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ReturnsEmptyList()
        {
            var companyId = "unauthorized";

            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((false, "Access denied"));

            var handler = new GetTeamsForCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _mapper,
                _loggerMock.Object);

            var result = await handler.Handle(new GetTeamsForCompanyQuery(companyId), default);

            Assert.Empty(result);
        }
    }
}
