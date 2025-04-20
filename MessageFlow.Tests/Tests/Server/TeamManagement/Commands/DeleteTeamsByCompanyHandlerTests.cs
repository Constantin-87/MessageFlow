using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.TeamManagement.Commands
{
    public class DeleteTeamsByCompanyHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<DeleteTeamsByCompanyHandler>> _loggerMock;

        public DeleteTeamsByCompanyHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<DeleteTeamsByCompanyHandler>>();
        }

        [Fact]
        public async Task Handle_ValidCompany_DeletesTeamsSuccessfully()
        {
            var companyId = "c1";
            var teams = new List<Team> { new() { Id = "t1", CompanyId = companyId } };

            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamsByCompanyIdAsync(companyId)).ReturnsAsync(teams);

            var handler = new DeleteTeamsByCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteTeamsByCompanyCommand(companyId), default);

            Assert.True(result);
            _unitOfWorkMock.Verify(x => x.Teams.DeleteTeams(teams), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var companyId = "unauth";
            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((false, "Unauthorized"));

            var handler = new DeleteTeamsByCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteTeamsByCompanyCommand(companyId), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_NoTeams_ReturnsTrue()
        {
            var companyId = "c1";
            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((true, ""));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamsByCompanyIdAsync(companyId)).ReturnsAsync(new List<Team>());

            var handler = new DeleteTeamsByCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteTeamsByCompanyCommand(companyId), default);

            Assert.True(result);
        }

        [Fact]
        public async Task Handle_Exception_ReturnsFalse()
        {
            var companyId = "c1";
            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((true, ""));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamsByCompanyIdAsync(companyId)).ThrowsAsync(new Exception("boom"));

            var handler = new DeleteTeamsByCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object
            );

            var result = await handler.Handle(new DeleteTeamsByCompanyCommand(companyId), default);

            Assert.False(result);
        }
    }
}
