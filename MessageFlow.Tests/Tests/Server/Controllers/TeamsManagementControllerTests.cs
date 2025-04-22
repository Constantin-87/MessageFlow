using Moq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MessageFlow.Server.Controllers;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.TeamManagement.Queries;
using MessageFlow.Server.MediatR.TeamManagement.Commands;

namespace MessageFlow.Tests.Tests.Server.Controllers
{
    public class TeamsManagementControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly TeamsManagementController _controller;

        public TeamsManagementControllerTests()
        {
            _controller = new TeamsManagementController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetTeamsForCompany_ReturnsOk()
        {
            var expected = new List<TeamDTO> { new() { Id = "t1", TeamName = "TeamA" } };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsForCompanyQuery>(), default))
                .ReturnsAsync(expected);

            var result = await _controller.GetTeamsForCompany("c1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, ok.Value);
        }

        [Fact]
        public async Task GetUsersForTeam_ReturnsOk()
        {
            var expected = new List<ApplicationUserDTO> { new() { Id = "u1", UserName = "user" } };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersForTeamQuery>(), default))
                .ReturnsAsync(expected);

            var result = await _controller.GetUsersForTeam("t1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, ok.Value);
        }

        [Fact]
        public async Task CreateTeam_ReturnsOk_WhenSuccess()
        {
            var team = new TeamDTO { Id = "t1", TeamName = "Team" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<AddTeamToCompanyCommand>(), default))
                .ReturnsAsync((true, string.Empty));

            var result = await _controller.CreateTeam(team);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Team created successfully", ok.Value);
        }

        [Fact]
        public async Task CreateTeam_ReturnsBadRequest_WhenFailed()
        {
            var team = new TeamDTO { Id = "t1", TeamName = "Team" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<AddTeamToCompanyCommand>(), default))
                .ReturnsAsync((false, "error"));

            var result = await _controller.CreateTeam(team);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("error", bad.Value);
        }

        [Fact]
        public async Task UpdateTeam_ReturnsOk_WhenSuccess()
        {
            var dto = new TeamDTO { Id = "t1", TeamName = "Team" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamCommand>(), default))
                .ReturnsAsync((true, string.Empty));

            var result = await _controller.UpdateTeam(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Team updated successfully", ok.Value);
        }

        [Fact]
        public async Task UpdateTeam_ReturnsBadRequest_WhenFailed()
        {
            var dto = new TeamDTO { Id = "t1", TeamName = "Team" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamCommand>(), default))
                .ReturnsAsync((false, "fail"));

            var result = await _controller.UpdateTeam(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("fail", bad.Value);
        }

        [Fact]
        public async Task DeleteTeam_ReturnsOk_WhenSuccess()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamByIdCommand>(), default))
                .ReturnsAsync((true, string.Empty));

            var result = await _controller.DeleteTeam("t1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Team deleted successfully", ok.Value);
        }

        [Fact]
        public async Task DeleteTeam_ReturnsBadRequest_WhenFailed()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamByIdCommand>(), default))
                .ReturnsAsync((false, "fail"));

            var result = await _controller.DeleteTeam("t1");

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("fail", bad.Value);
        }
    }
}