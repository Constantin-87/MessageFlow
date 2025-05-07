using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.UserManagement.QueryHandlers;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MessageFlow.Shared.DTOs;
using Moq;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.UserManagement.Queries
{
    public class GetUserByIdHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<GetUserByIdHandler>> _loggerMock;

        public GetUserByIdHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
            _mapperMock = new Mock<IMapper>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<GetUserByIdHandler>>();
        }

        [Fact]
        public async Task Handle_UserExistsAndAuthorized_ReturnsDto()
        {
            var userId = "u1";
            var user = new ApplicationUser { Id = userId, CompanyId = "c1" };
            var roles = new List<string> { "Admin" };

            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);
            _authHelperMock.Setup(a => a.UserManagementAccess("c1", roles)).ReturnsAsync((true, ""));
            _mapperMock.Setup(m => m.Map<ApplicationUserDTO>(user)).Returns(new ApplicationUserDTO { Id = userId });

            var handler = new GetUserByIdHandler(_userManagerMock.Object, _mapperMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new GetUserByIdQuery(userId), default);

            Assert.NotNull(result);
            Assert.Equal(userId, result!.Id);
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsNull()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

            var handler = new GetUserByIdHandler(_userManagerMock.Object, _mapperMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new GetUserByIdQuery("missing"), default);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_UnauthorizedAccess_ReturnsNull()
        {
            var user = new ApplicationUser { Id = "u2", CompanyId = "c1" };
            var roles = new List<string> { "Admin" };

            _userManagerMock.Setup(m => m.FindByIdAsync("u2")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);
            _authHelperMock.Setup(a => a.UserManagementAccess("c1", roles)).ReturnsAsync((false, "Not allowed"));

            var handler = new GetUserByIdHandler(_userManagerMock.Object, _mapperMock.Object, _authHelperMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new GetUserByIdQuery("u2"), default);

            Assert.Null(result);
        }
    }
}
