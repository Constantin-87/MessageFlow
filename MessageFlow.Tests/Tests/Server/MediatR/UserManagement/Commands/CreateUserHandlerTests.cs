using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.UserManagement.CommandHandlers;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.UserManagement.Commands
{
    public class CreateUserHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<CreateUserHandler>> _loggerMock;

        public CreateUserHandlerTests()
        {
            _userManagerMock = TestDbContextFactory.CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable());
            _mapperMock = new Mock<IMapper>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<CreateUserHandler>>();
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesUser()
        {
            var dto = new ApplicationUserDTO
            {
                UserName = "testuser",
                UserEmail = "test@example.com",
                CompanyId = "c1",
                Role = "Admin",
                NewPassword = "Password123!"
            };

            var user = new ApplicationUser { UserName = dto.UserName, Email = dto.UserEmail, CompanyId = dto.CompanyId };

            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));

            _mapperMock.Setup(m => m.Map<ApplicationUser>(dto)).Returns(user);

            _userManagerMock.Setup(u => u.CreateAsync(user, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(user, dto.Role))
                .ReturnsAsync(IdentityResult.Success);

            var handler = new CreateUserHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateUserCommand(dto), default);

            Assert.True(result.success);
            Assert.Equal("User created successfully", result.errorMessage);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsError()
        {
            var dto = new ApplicationUserDTO
            {
                UserName = "unauth",
                CompanyId = "x",
                Role = "SuperAdmin",
                NewPassword = "pass"
            };

            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((false, "Not allowed"));

            var handler = new CreateUserHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Equal("Not allowed", result.errorMessage);
        }

        [Fact]
        public async Task Handle_CreateFails_ReturnsError()
        {
            var dto = new ApplicationUserDTO
            {
                UserName = "failuser",
                UserEmail = "fail@user.com",
                CompanyId = "c2",
                Role = "Admin",
                NewPassword = "bad"
            };

            var user = new ApplicationUser();

            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));

            _mapperMock.Setup(m => m.Map<ApplicationUser>(dto)).Returns(user);

            _userManagerMock.Setup(u => u.CreateAsync(user, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Weak password" }));

            var handler = new CreateUserHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Contains("Weak password", result.errorMessage);
        }

        [Fact]
        public async Task Handle_AddToRoleFails_ReturnsError()
        {
            var dto = new ApplicationUserDTO
            {
                UserName = "rolefail",
                UserEmail = "role@fail.com",
                CompanyId = "c2",
                Role = "Admin",
                NewPassword = "GoodPassword123"
            };

            var user = new ApplicationUser();

            _authHelperMock.Setup(x => x.UserManagementAccess(dto.CompanyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));

            _mapperMock.Setup(m => m.Map<ApplicationUser>(dto)).Returns(user);

            _userManagerMock.Setup(u => u.CreateAsync(user, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(user, dto.Role))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assign error" }));

            var handler = new CreateUserHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _authHelperMock.Object
            );

            var result = await handler.Handle(new CreateUserCommand(dto), default);

            Assert.False(result.success);
            Assert.Contains("Role assign error", result.errorMessage);
        }
    }
}
