using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.TeamManagement.QueryHandlers;
using MessageFlow.Server.MediatR.TeamManagement.Queries;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.TeamManagement.Queries
{
    public class GetUsersForTeamHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<GetUsersForTeamHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public GetUsersForTeamHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<GetUsersForTeamHandler>>();

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsUsers()
        {
            var teamId = "team1";
            var companyId = "comp1";

            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(teamId))
                .ReturnsAsync(new Team { Id = teamId, CompanyId = companyId });

            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(x => x.Teams.GetUsersByTeamIdAsync(teamId))
                .ReturnsAsync(new List<ApplicationUser> { new() { Id = "u1" } });

            var handler = new GetUsersForTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _mapper, _loggerMock.Object);
            var result = await handler.Handle(new GetUsersForTeamQuery(teamId), default);

            Assert.Single(result);
            Assert.Equal("u1", result.First().Id);
        }

        [Fact]
        public async Task Handle_TeamNotFound_ReturnsEmptyList()
        {
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync("bad"))
                .ReturnsAsync((Team?)null);

            var handler = new GetUsersForTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _mapper, _loggerMock.Object);
            var result = await handler.Handle(new GetUsersForTeamQuery("bad"), default);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsEmptyList()
        {
            var teamId = "t1";
            var team = new Team { Id = teamId, CompanyId = "unauth" };

            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(teamId))
                .ReturnsAsync(team);

            _authHelperMock.Setup(x => x.TeamAccess(team.CompanyId))
                .ReturnsAsync((false, "unauthorized"));

            var handler = new GetUsersForTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _mapper, _loggerMock.Object);
            var result = await handler.Handle(new GetUsersForTeamQuery(teamId), default);

            Assert.Empty(result);
        }
    }
}
