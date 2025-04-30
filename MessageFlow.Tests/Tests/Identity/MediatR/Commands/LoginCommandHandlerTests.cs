using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();

        private LoginCommandHandler CreateHandler(IQueryable<ApplicationUser> users, Action<Mock<UserManager<ApplicationUser>>, ApplicationUser>? configureMocks = null)
        {
            var userManagerMock = TestDbContextFactory.CreateMockUserManager(users);

            if (users.Any())
            {
                var user = users.First();
                configureMocks?.Invoke(userManagerMock, user);
            }

            return new LoginCommandHandler(userManagerMock.Object, _tokenServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailure()
        {
            var handler = CreateHandler(Enumerable.Empty<ApplicationUser>().AsQueryable());

            var result = await handler.Handle(new LoginCommand("nonexistent", "pass"), CancellationToken.None);

            Assert.False(result.Item1);
            Assert.Equal("Invalid username or password.", result.Item4);
        }

        [Fact]
        public async Task Handle_InvalidPassword_ReturnsFailure()
        {
            var user = new ApplicationUser { UserName = "user1" };

            var handler = CreateHandler(new[] { user }.AsQueryable(), (mock, u) =>
            {
                mock.Setup(x => x.CheckPasswordAsync(u, "wrongpass")).ReturnsAsync(false);
            });

            var result = await handler.Handle(new LoginCommand("user1", "wrongpass"), CancellationToken.None);

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

            var handler = CreateHandler(new[] { user }.AsQueryable(), (mock, u) =>
            {
                mock.Setup(x => x.CheckPasswordAsync(u, "correct")).ReturnsAsync(true);
                mock.Setup(x => x.GetRolesAsync(u)).ReturnsAsync(new[] { "Admin" });
                mock.Setup(x => x.UpdateAsync(u)).ReturnsAsync(IdentityResult.Success);
            });

            _tokenServiceMock.Setup(x => x.GenerateJwtTokenAsync(user)).ReturnsAsync("jwt-token");
            _tokenServiceMock.Setup(x => x.SetRefreshTokenAsync(user)).ReturnsAsync("refresh-token");

            var result = await handler.Handle(new LoginCommand("user1", "correct"), CancellationToken.None);

            Assert.True(result.Item1);
            Assert.Equal("jwt-token", result.Item2);
            Assert.Equal("refresh-token", result.Item3);
            Assert.Equal("user1", result.Item5!.UserName);
            Assert.Equal("Admin", result.Item5.Role);
            Assert.Equal("TestCo", result.Item5.CompanyDTO?.CompanyName);
        }
    }
}