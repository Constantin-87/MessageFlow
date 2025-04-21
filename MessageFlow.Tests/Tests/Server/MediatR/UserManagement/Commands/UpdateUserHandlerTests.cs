using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatR.UserManagement.CommandHandlers;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Tests.Tests.Server.MediatR.UserManagement.Commands
{
    public class UpdateUserHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<UpdateUserHandler>> _loggerMock;

        public UpdateUserHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<UpdateUserHandler>>();
        }

        [Fact]
        public async Task Handle_ValidUpdate_ReturnsSuccess()
        {
            var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };
            var dto = new ApplicationUserDTO
            {
                Id = "u1",
                CompanyId = "c1",
                Role = "Admin",
                UserName = "updated",
                UserEmail = "test@example.com",
                PhoneNumber = "123",
                LockoutEnabled = false
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(dto.Id)).ReturnsAsync(user);
            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, ""));
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Agent" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(user, dto.Role))
                .ReturnsAsync(IdentityResult.Success);

            var handler = new UpdateUserHandler(_userManagerMock.Object, _loggerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new UpdateUserCommand(dto), default);

            Assert.True(result.success);
            Assert.Equal("User updated successfully", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsError()
        {
            var dto = new ApplicationUserDTO { Id = "u1", CompanyId = "c1", Role = "Admin" };

            _userManagerMock.Setup(x => x.FindByIdAsync(dto.Id)).ReturnsAsync(new ApplicationUser());
            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((false, "No access"));

            var handler = new UpdateUserHandler(_userManagerMock.Object, _loggerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new UpdateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Equal("No access", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsError()
        {
            var dto = new ApplicationUserDTO { Id = "notfound", CompanyId = "c1", Role = "Admin" };
            _userManagerMock.Setup(x => x.FindByIdAsync(dto.Id)).ReturnsAsync((ApplicationUser?)null);

            var handler = new UpdateUserHandler(_userManagerMock.Object, _loggerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new UpdateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Equal("Target user not found.", result.errorMessage);
        }

        [Fact]
        public async Task Handle_UpdateFails_ReturnsError()
        {
            var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };
            var dto = new ApplicationUserDTO { Id = "u1", CompanyId = "c1", Role = "Admin" };

            _userManagerMock.Setup(x => x.FindByIdAsync(dto.Id)).ReturnsAsync(user);
            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, ""));
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            var handler = new UpdateUserHandler(_userManagerMock.Object, _loggerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new UpdateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Contains("Update failed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_PasswordChangeFails_ReturnsError()
        {
            var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };
            var dto = new ApplicationUserDTO
            {
                Id = "u1",
                CompanyId = "c1",
                Role = "Admin",
                NewPassword = "newpass123"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(dto.Id)).ReturnsAsync(user);
            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, ""));
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "token", dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password error" }));

            var handler = new UpdateUserHandler(_userManagerMock.Object, _loggerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new UpdateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Contains("Password error", result.errorMessage);
        }
    }
}
