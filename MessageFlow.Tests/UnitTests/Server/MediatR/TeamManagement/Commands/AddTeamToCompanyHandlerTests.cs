using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.TeamManagement.CommandHandlers;
using MessageFlow.Server.MediatR.TeamManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.TeamManagement.Commands
{
    public class AddTeamToCompanyHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<AddTeamToCompanyHandler>> _loggerMock;

        public AddTeamToCompanyHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<AddTeamToCompanyHandler>>();
        }

        [Fact]
        public async Task Handle_ValidRequest_AddsTeamSuccessfully()
        {
            var companyId = "c1";
            var teamDto = new TeamDTO
            {
                CompanyId = companyId,
                TeamName = "Support",
                AssignedUsersDTO = new List<ApplicationUserDTO>
                {
                    new() { Id = "u1" },
                    new() { Id = "u2" }
                }
            };

            _authHelperMock.Setup(x => x.TeamAccess(companyId))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(x => x.ApplicationUsers.GetListOfEntitiesByIdStringAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<ApplicationUser> { new() { Id = "u1" }, new() { Id = "u2" } });

            _unitOfWorkMock.Setup(x => x.Teams.AddEntityAsync(It.IsAny<Team>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var handler = new AddTeamToCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new AddTeamToCompanyCommand(teamDto), default);

            Assert.True(result.success);
            Assert.Equal("Team added successfully.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var teamDto = new TeamDTO { CompanyId = "c2" };

            _authHelperMock.Setup(x => x.TeamAccess(teamDto.CompanyId))
                .ReturnsAsync((false, "Unauthorized"));

            var handler = new AddTeamToCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new AddTeamToCompanyCommand(teamDto), default);

            Assert.False(result.success);
            Assert.Equal("Unauthorized", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UsersNotFound_ReturnsError()
        {
            var companyId = "c1";
            var teamDto = new TeamDTO
            {
                CompanyId = companyId,
                AssignedUsersDTO = new List<ApplicationUserDTO> { new() { Id = "u1" } }
            };

            _authHelperMock.Setup(x => x.TeamAccess(companyId))
                .ReturnsAsync((true, string.Empty));

            _unitOfWorkMock.Setup(x => x.ApplicationUsers.GetListOfEntitiesByIdStringAsync(It.IsAny<List<string>>()))
                .ReturnsAsync((List<ApplicationUser>?)null);

            var handler = new AddTeamToCompanyHandler(
                _unitOfWorkMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new AddTeamToCompanyCommand(teamDto), default);

            Assert.False(result.success);
            Assert.Equal("An error occurred while retrieving the users.", result.errorMessage);
        }
    }
}
