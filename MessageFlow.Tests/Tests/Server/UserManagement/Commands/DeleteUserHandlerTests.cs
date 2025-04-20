using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Repositories;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.UserManagement.Commands
{
    public class DeleteUserHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ITeamRepository> _teamRepoMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<DeleteUserHandler>> _loggerMock;

        public DeleteUserHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            _teamRepoMock = new Mock<ITeamRepository>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<DeleteUserHandler>>();
        }

        [Fact]
        public async Task Handle_UserFoundAndAuthorized_DeletesSuccessfully()
        {
            var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };

            _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Agent" });
            _authHelperMock.Setup(a => a.UserManagementAccess("c1", It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));
            _teamRepoMock.Setup(t => t.RemoveUserFromAllTeamsAsync(user.Id)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var handler = new DeleteUserHandler(_userManagerMock.Object, _teamRepoMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUserCommand(user.Id), default);

            Assert.True(result);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFalse()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

            var handler = new DeleteUserHandler(_userManagerMock.Object, _teamRepoMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUserCommand("missing"), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var user = new ApplicationUser { Id = "u2", CompanyId = "x" };

            _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "SuperAdmin" });
            _authHelperMock.Setup(a => a.UserManagementAccess("x", It.IsAny<List<string>>()))
                .ReturnsAsync((false, "Unauthorized"));

            var handler = new DeleteUserHandler(_userManagerMock.Object, _teamRepoMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUserCommand(user.Id), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_DeleteFails_ReturnsFalse()
        {
            var user = new ApplicationUser { Id = "u3", CompanyId = "c3" };

            _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Agent" });
            _authHelperMock.Setup(a => a.UserManagementAccess("c3", It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));
            _teamRepoMock.Setup(t => t.RemoveUserFromAllTeamsAsync(user.Id)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(m => m.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed to delete" }));

            var handler = new DeleteUserHandler(_userManagerMock.Object, _teamRepoMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUserCommand(user.Id), default);

            Assert.False(result);
        }
    }
}
