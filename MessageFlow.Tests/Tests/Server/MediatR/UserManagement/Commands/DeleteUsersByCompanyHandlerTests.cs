using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.UserManagement.CommandHandlers;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.UserManagement.Commands
{
    public class DeleteUsersByCompanyHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<DeleteUsersByCompanyHandler>> _loggerMock;

        public DeleteUsersByCompanyHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<DeleteUsersByCompanyHandler>>();
        }

        [Fact]
        public async Task Handle_UsersExistAndAuthorized_DeletesSuccessfully()
        {
            var companyId = "c1";
            var users = new List<ApplicationUser>
            {
                new() { Id = "u1", CompanyId = companyId },
                new() { Id = "u2", CompanyId = companyId }
            };

            var queryable = users.AsQueryable().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(queryable.Object);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Agent" });
            _authHelperMock.Setup(x => x.UserManagementAccess(companyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, ""));
            _userManagerMock.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var handler = new DeleteUsersByCompanyHandler(_userManagerMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUsersByCompanyCommand(companyId), default);

            Assert.True(result);
        }

        [Fact]
        public async Task Handle_NoUsersFound_ReturnsTrue()
        {
            var queryable = new List<ApplicationUser>().AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(x => x.Users).Returns(queryable.Object);

            var handler = new DeleteUsersByCompanyHandler(_userManagerMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUsersByCompanyCommand("empty"), default);

            Assert.True(result);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsFalse()
        {
            var users = new List<ApplicationUser> { new() { Id = "u1", CompanyId = "x" } };
            var queryable = users.AsQueryable().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(queryable.Object);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Admin" });

            _authHelperMock.Setup(x => x.UserManagementAccess("x", It.IsAny<List<string>>()))
                .ReturnsAsync((false, "No access"));

            var handler = new DeleteUsersByCompanyHandler(_userManagerMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUsersByCompanyCommand("x"), default);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_DeleteFails_StillReturnsTrue()
        {
            var companyId = "c2";
            var users = new List<ApplicationUser>
            {
                new() { Id = "u1", CompanyId = companyId }
            };

            var queryable = users.AsQueryable().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(queryable.Object);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Agent" });
            _authHelperMock.Setup(x => x.UserManagementAccess(companyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, ""));
            _userManagerMock.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete error" }));

            var handler = new DeleteUsersByCompanyHandler(_userManagerMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new DeleteUsersByCompanyCommand(companyId), default);

            Assert.True(result);
        }
    }
}
