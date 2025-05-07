using Microsoft.AspNetCore.Identity;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.UserManagement.QueryHandlers;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using Moq;
using MockQueryable.Moq;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.UserManagement.Queries
{
    public class GetAvailableRolesHandlerTests
    {
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<IAuthorizationHelper> _authHelperMock;

        public GetAvailableRolesHandlerTests()
        {
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null, null, null, null);
            _authHelperMock = new Mock<IAuthorizationHelper>();
        }

        [Fact]
        public async Task Handle_SuperAdmin_ReturnsAllRoles()
        {
            var roles = new List<IdentityRole>
            {
                new("Admin"), new("Agent"), new("SuperAdmin")
            };
            var dbSetMock = roles.AsQueryable().BuildMockDbSet();

            _roleManagerMock.Setup(x => x.Roles).Returns(dbSetMock.Object);
            _authHelperMock.Setup(x => x.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((true, null, true, ""));

            var handler = new GetAvailableRolesHandler(_roleManagerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new GetAvailableRolesQuery(), default);

            Assert.Equal(3, result.Count);
            Assert.Contains("SuperAdmin", result);
        }

        [Fact]
        public async Task Handle_NonSuperAdmin_ExcludesSuperAdminRole()
        {
            var roles = new List<IdentityRole>
            {
                new("Admin"), new("Agent"), new("SuperAdmin")
            };
            var dbSetMock = roles.AsQueryable().BuildMockDbSet();

            _roleManagerMock.Setup(x => x.Roles).Returns(dbSetMock.Object);
            _authHelperMock.Setup(x => x.CompanyAccess(It.IsAny<string>()))
                .ReturnsAsync((true, "c1", false, ""));

            var handler = new GetAvailableRolesHandler(_roleManagerMock.Object, _authHelperMock.Object);
            var result = await handler.Handle(new GetAvailableRolesQuery(), default);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain("SuperAdmin", result);
        }
    }
}
