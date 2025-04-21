using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;

        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            _tokenServiceMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

            _handler = new LoginCommandHandler(_userManagerMock.Object, _tokenServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            var users = new List<ApplicationUser>().AsQueryable().BuildMock().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(users.Object);

            var result = await _handler.Handle(new LoginCommand("nonexistent", "pass"), CancellationToken.None);

            Assert.False(result.Item1);
            Assert.Equal("Invalid username or password.", result.Item4);
        }

        [Fact]
        public async Task Handle_InvalidPassword_ReturnsFailure()
        {
            var user = new ApplicationUser { UserName = "user1" };
            var users = new List<ApplicationUser> { user }.AsQueryable().BuildMock().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(users.Object);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrongpass")).ReturnsAsync(false);

            var result = await _handler.Handle(new LoginCommand("user1", "wrongpass"), CancellationToken.None);

            Assert.False(result.Item1);
            Assert.Equal("Invalid username or password.", result.Item4);
        }

        [Fact]
        public async Task Handle_ValidLogin_ReturnsSuccess()
        {
            var user = new ApplicationUser
            {
                Id = "1",
                UserName = "user1",
                CompanyId = "comp1",
                Company = new Company { Id = "comp1", CompanyName = "TestCo" }
            };

            var users = new List<ApplicationUser> { user }.AsQueryable().BuildMock().BuildMockDbSet();

            _userManagerMock.Setup(x => x.Users).Returns(users.Object);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new[] { "Admin" });
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _tokenServiceMock.Setup(x => x.GenerateJwtTokenAsync(user)).ReturnsAsync("jwt-token");
            _tokenServiceMock.Setup(x => x.SetRefreshTokenAsync(user)).ReturnsAsync("refresh-token");

            var result = await _handler.Handle(new LoginCommand("user1", "correct"), CancellationToken.None);

            Assert.True(result.Item1);
            Assert.Equal("jwt-token", result.Item2);
            Assert.Equal("refresh-token", result.Item3);
            Assert.Equal("user1", result.Item5!.UserName);
            Assert.Equal("Admin", result.Item5.Role);
            Assert.Equal("TestCo", result.Item5.CompanyDTO?.CompanyName);
        }
    }
}