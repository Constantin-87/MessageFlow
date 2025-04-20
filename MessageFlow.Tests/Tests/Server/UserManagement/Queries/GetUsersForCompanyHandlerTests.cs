using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;

namespace MessageFlow.Tests.Tests.Server.UserManagement.Queries
{
    public class GetUsersForCompanyHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;
        private readonly Mock<ILogger<GetUsersForCompanyHandler>> _loggerMock;

        public GetUsersForCompanyHandlerTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mapperMock = new Mock<IMapper>();
            _authHelperMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<GetUsersForCompanyHandler>>();
        }

        [Fact]
        public async Task Handle_ReturnsAuthorizedUsers()
        {
            // Arrange
            var companyId = "company-123";
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "u1", CompanyId = companyId },
                new ApplicationUser { Id = "u2", CompanyId = companyId }
            };

            var dbSetMock = users.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(dbSetMock.Object);
            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Admin" });

            _authHelperMock.Setup(x => x.UserManagementAccess(companyId, It.IsAny<List<string>>()))
                .ReturnsAsync((true, string.Empty));

            _mapperMock.Setup(m => m.Map<ApplicationUserDTO>(It.IsAny<ApplicationUser>()))
                .Returns<ApplicationUser>(u => new ApplicationUserDTO { Id = u.Id });

            var handler = new GetUsersForCompanyHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            // Act
            var result = await handler.Handle(new GetUsersForCompanyQuery(companyId), default);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, dto => Assert.Contains(dto.Id, new[] { "u1", "u2" }));
        }

        [Fact]
        public async Task Handle_SkipsUnauthorizedUsers()
        {
            var companyId = "company-123";
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "u1", CompanyId = companyId }
            };

            var dbSetMock = users.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(dbSetMock.Object);
            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Admin" });

            _authHelperMock.Setup(x => x.UserManagementAccess(companyId, It.IsAny<List<string>>()))
                .ReturnsAsync((false, "Not allowed"));

            var handler = new GetUsersForCompanyHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetUsersForCompanyQuery(companyId), default);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_NoUsersFound_ReturnsEmptyList()
        {
            var dbSetMock = new List<ApplicationUser>().AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(dbSetMock.Object);

            var handler = new GetUsersForCompanyHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetUsersForCompanyQuery("any-company"), default);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_Exception_ReturnsEmptyList()
        {
            _userManagerMock.Setup(u => u.Users).Throws(new Exception("DB error"));

            var handler = new GetUsersForCompanyHandler(
                _userManagerMock.Object,
                _mapperMock.Object,
                _authHelperMock.Object,
                _loggerMock.Object);

            var result = await handler.Handle(new GetUsersForCompanyQuery("fail-company"), default);

            Assert.Empty(result);
        }
    }
}
