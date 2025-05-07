using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.UserManagement.QueryHandlers;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MessageFlow.Infrastructure.Mappings;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.Server.MediatR.UserManagement.Queries
{
    public class GetAllUsersHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IAuthorizationHelper> _authMock;
        private readonly Mock<ILogger<GetAllUsersHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public GetAllUsersHandlerTests()
        {
            _userManagerMock = UnitTestFactory.CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable());
            _authMock = new Mock<IAuthorizationHelper>();
            _loggerMock = new Mock<ILogger<GetAllUsersHandler>>();

            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task Handle_SuperAdmin_ReturnsAllUsers()
        {
            var users = new List<ApplicationUser>
            {
                new() { Id = "u1", UserName = "super", CompanyId = "c1", Company = new Company() }
            };
            var userDbSet = users.AsQueryable().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(userDbSet.Object);
            _userManagerMock.Setup(x => x.FindByIdAsync("u1")).ReturnsAsync(users[0]);
            _userManagerMock.Setup(x => x.GetRolesAsync(users[0])).ReturnsAsync(new List<string> { "SuperAdmin" });

            _authMock.Setup(x => x.CompanyAccess(string.Empty))
                .ReturnsAsync((true, null, true, ""));

            var handler = new GetAllUsersHandler(_userManagerMock.Object, _mapper, _authMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new GetAllUsersQuery(), default);

            Assert.Single(result);
            Assert.Equal("SuperAdmin", result[0].Role);
        }

        [Fact]
        public async Task Handle_Admin_ReturnsUsersInSameCompany()
        {
            var users = new List<ApplicationUser>
            {
                new() { Id = "u2", UserName = "admin", CompanyId = "c2", Company = new Company() }
            };
            var userDbSet = users.AsQueryable().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(userDbSet.Object);
            _userManagerMock.Setup(x => x.FindByIdAsync("u2")).ReturnsAsync(users[0]);
            _userManagerMock.Setup(x => x.GetRolesAsync(users[0])).ReturnsAsync(new List<string> { "Admin" });

            _authMock.Setup(x => x.CompanyAccess(string.Empty))
                .ReturnsAsync((true, "c2", false, ""));

            var handler = new GetAllUsersHandler(_userManagerMock.Object, _mapper, _authMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new GetAllUsersQuery(), default);

            Assert.Single(result);
            Assert.Equal("Admin", result[0].Role);
        }

        [Fact]
        public async Task Handle_Unauthorized_ReturnsEmptyList()
        {
            _authMock.Setup(x => x.CompanyAccess(string.Empty))
                .ReturnsAsync((false, null, false, "No context"));

            var handler = new GetAllUsersHandler(_userManagerMock.Object, _mapper, _authMock.Object, _loggerMock.Object);
            var result = await handler.Handle(new GetAllUsersQuery(), default);

            Assert.Empty(result);
        }
    }
}
