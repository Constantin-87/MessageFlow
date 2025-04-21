using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.TeamManagement.CommandHandlers;
using MessageFlow.Server.MediatR.TeamManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.TeamManagement.Commands
{
    public class UpdateTeamHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<UpdateTeamHandler>> _loggerMock;

        public UpdateTeamHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<UpdateTeamHandler>>();
        }

        [Fact]
        public async Task Handle_ValidUpdate_ReturnsSuccess()
        {
            var teamId = "team1";
            var companyId = "comp1";
            var teamDto = new TeamDTO
            {
                Id = teamId,
                CompanyId = companyId,
                TeamName = "Updated",
                TeamDescription = "Desc",
                AssignedUsersDTO = new List<ApplicationUserDTO> { new() { Id = "u1" } }
            };

            var team = new Team { Id = teamId, CompanyId = companyId, Users = new List<ApplicationUser>() };

            _authHelperMock.Setup(x => x.TeamAccess(companyId)).ReturnsAsync((true, ""));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(teamId)).ReturnsAsync(team);
            _unitOfWorkMock.Setup(x => x.ApplicationUsers.GetListOfEntitiesByIdStringAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<ApplicationUser> { new() { Id = "u1" } });
            _unitOfWorkMock.Setup(x => x.Teams.UpdateEntityAsync(team)).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var handler = new UpdateTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new UpdateTeamCommand(teamDto), default);

            Assert.True(result.success);
            Assert.Equal("Team updated successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_TeamNotFound_ReturnsError()
        {
            var dto = new TeamDTO { Id = "notfound", CompanyId = "c1" };

            _authHelperMock.Setup(x => x.TeamAccess(dto.CompanyId)).ReturnsAsync((true, ""));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(dto.Id)).ReturnsAsync((Team?)null);

            var handler = new UpdateTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new UpdateTeamCommand(dto), default);

            Assert.False(result.success);
            Assert.Equal("Team not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_InvalidUsers_ReturnsError()
        {
            var dto = new TeamDTO
            {
                Id = "t1",
                CompanyId = "c1",
                AssignedUsersDTO = new List<ApplicationUserDTO> { new() { Id = "bad" } }
            };

            var team = new Team { Id = "t1", CompanyId = "c1", Users = new List<ApplicationUser>() };

            _authHelperMock.Setup(x => x.TeamAccess(dto.CompanyId)).ReturnsAsync((true, ""));
            _unitOfWorkMock.Setup(x => x.Teams.GetTeamByIdAsync(dto.Id)).ReturnsAsync(team);
            _unitOfWorkMock.Setup(x => x.ApplicationUsers.GetListOfEntitiesByIdStringAsync(It.IsAny<List<string>>()))
                .ReturnsAsync((List<ApplicationUser>?)null);

            var handler = new UpdateTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new UpdateTeamCommand(dto), default);

            Assert.False(result.success);
            Assert.Equal("No valid users found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsError()
        {
            var dto = new TeamDTO { Id = "t1", CompanyId = "c1" };

            _authHelperMock.Setup(x => x.TeamAccess(dto.CompanyId)).ReturnsAsync((false, "Unauthorized"));

            var handler = new UpdateTeamHandler(_unitOfWorkMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new UpdateTeamCommand(dto), default);

            Assert.False(result.success);
            Assert.Equal("Unauthorized", result.errorMessage);
        }
    }
}
