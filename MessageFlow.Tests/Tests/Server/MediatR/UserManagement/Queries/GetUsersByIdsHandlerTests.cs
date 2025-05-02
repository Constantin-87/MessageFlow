using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.UserManagement.QueryHandlers;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;

namespace MessageFlow.Tests.Tests.Server.MediatR.UserManagement.Queries
{
    public class GetUsersByIdsHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<GetUsersByIdsHandler>> _loggerMock;

        public GetUsersByIdsHandlerTests()
        {
            _userManagerMock = TestDbContextFactory.CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable());
            _mapperMock = new Mock<IMapper>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<GetUsersByIdsHandler>>();
        }

        [Fact]
        public async Task Handle_ReturnsAuthorizedUsers()
        {
            var companyId = "c1";
            var userList = new List<ApplicationUser>
            {
                new() { Id = "u1", CompanyId = companyId },
                new() { Id = "u2", CompanyId = companyId }
            };

            var dbSet = userList.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(dbSet.Object);

            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Admin" });

            _authHelperMock.Setup(x => x.UserManagementAccess(companyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));

            _mapperMock.Setup(m => m.Map<ApplicationUserDTO>(It.IsAny<ApplicationUser>()))
                .Returns<ApplicationUser>(u => new ApplicationUserDTO { Id = u.Id });

            var handler = new GetUsersByIdsHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetUsersByIdsQuery(new List<string> { "u1", "u2" }), default);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Handle_SkipsUnauthorizedUsers()
        {
            var companyId = "c2";
            var userList = new List<ApplicationUser> { new() { Id = "u3", CompanyId = companyId } };

            var dbSet = userList.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(dbSet.Object);

            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "SuperAdmin" });

            _authHelperMock.Setup(x => x.UserManagementAccess(companyId, It.IsAny<List<string>>()))
                .ReturnsAsync((false, "Unauthorized"));

            var handler = new GetUsersByIdsHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetUsersByIdsQuery(new List<string> { "u3" }), default);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_Exception_ReturnsEmptyList()
        {
            _userManagerMock.Setup(u => u.Users).Throws(new Exception("DB error"));

            var handler = new GetUsersByIdsHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetUsersByIdsQuery(new List<string> { "error" }), default);

            Assert.Empty(result);
        }
    }
}