using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.TeamManagement.Commands
{
    public class DeleteTeamByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<DeleteTeamByIdHandler>> _loggerMock;

        public DeleteTeamByIdHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<DeleteTeamByIdHandler>>();
        }

        [Fact]
        public async Task Handle_TeamFoundAndAuthorized_DeletesSuccessfully()
        {
            var team = new Team { Id = "t1", CompanyId = "c1" };

            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(team.Id))
                .ReturnsAsync(team);
            _authHelperMock.Setup(x => x.TeamAccess(team.CompanyId))
                .ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var handler = new DeleteTeamByIdHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new DeleteTeamByIdCommand(team.Id), default);

            Assert.True(result.success);
            Assert.Equal("Team deleted successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_TeamNotFound_ReturnsFalse()
        {
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync("notfound"))
                .ReturnsAsync((Team?)null);

            var handler = new DeleteTeamByIdHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new DeleteTeamByIdCommand("notfound"), default);

            Assert.False(result.success);
            Assert.Equal("Team not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var team = new Team { Id = "t2", CompanyId = "c2" };

            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(team.Id))
                .ReturnsAsync(team);
            _authHelperMock.Setup(x => x.TeamAccess(team.CompanyId))
                .ReturnsAsync((false, "Not allowed"));

            var handler = new DeleteTeamByIdHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new DeleteTeamByIdCommand(team.Id), default);

            Assert.False(result.success);
            Assert.Equal("Not allowed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_ExceptionDuringDelete_ReturnsFalse()
        {
            var team = new Team { Id = "t3", CompanyId = "c3" };

            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(team.Id))
                .ReturnsAsync(team);
            _authHelperMock.Setup(x => x.TeamAccess(team.CompanyId))
                .ReturnsAsync((true, string.Empty));
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
                .ThrowsAsync(new Exception("Boom"));

            var handler = new DeleteTeamByIdHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new DeleteTeamByIdCommand(team.Id), default);

            Assert.False(result.success);
            Assert.Equal("An error occurred while deleting the team.", result.errorMessage);
        }
    }
}
