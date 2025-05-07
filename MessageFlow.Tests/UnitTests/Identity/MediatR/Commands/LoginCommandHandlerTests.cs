using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.Models;
using MessageFlow.Identity.Services.Interfaces;
using MessageFlow.Tests.Helpers.Factories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageFlow.Tests.UnitTests.Identity.MediatR.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();

        private LoginCommandHandler CreateHandler(IQueryable<ApplicationUser> users, Action<Mock<UserManager<ApplicationUser>>, ApplicationUser>? configureMocks = null)
        {
            var userManagerMock = UnitTestFactory.CreateMockUserManager(users);

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

            var result = await handler.Handle(new LoginCommand(new LoginRequest { Username = "nonexistent", Password = "pass" }), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_InvalidPassword_ReturnsFailure()
        {
            var user = new ApplicationUser { UserName = "user1" };

            var handler = CreateHandler(new[] { user }.AsQueryable(), (mock, u) =>
            {
                mock.Setup(x => x.CheckPasswordAsync(u, "wrongpass")).ReturnsAsync(false);
            });

            var result = await handler.Handle(new LoginCommand(new LoginRequest { Username = "user1", Password = "wrongpass" }), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
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

            var result = await handler.Handle(new LoginCommand(new LoginRequest { Username = "user1", Password = "correct" }), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("jwt-token", result.Token);
            Assert.Equal("refresh-token", result.RefreshToken);
            Assert.Equal("user1", result.User!.UserName);
            Assert.Equal("Admin", result.User.Role);
            Assert.Equal("TestCo", result.User.CompanyDTO?.CompanyName);
        }

        [Fact]
        public async Task Handle_LockedOutUser_ReturnsFailure()
        {
            var user = new ApplicationUser { UserName = "lockedUser" };

            var handler = CreateHandler(new[] { user }.AsQueryable(), (mock, u) =>
            {
                mock.Setup(x => x.IsLockedOutAsync(u)).ReturnsAsync(true);
                mock.Setup(x => x.GetLockoutEndDateAsync(u)).ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(5));
            });

            var result = await handler.Handle(new LoginCommand(new LoginRequest { Username = "lockedUser", Password = "irrelevant" }), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Account is locked.", result.ErrorMessage);
            Assert.NotNull(result.LockoutEnd);
        }

        [Fact]
        public async Task Handle_FailedPassword_IncrementsAttempts()
        {
            var user = new ApplicationUser { UserName = "user2" };

            var handler = CreateHandler(new[] { user }.AsQueryable(), (mock, u) =>
            {
                mock.Setup(x => x.CheckPasswordAsync(u, "badpass")).ReturnsAsync(false);
                mock.Setup(x => x.AccessFailedAsync(u)).ReturnsAsync(IdentityResult.Success);
                mock.Setup(x => x.GetAccessFailedCountAsync(u)).ReturnsAsync(3);
            });

            var result = await handler.Handle(new LoginCommand(new LoginRequest { Username = "user2", Password = "badpass" }), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
        }
    }
}