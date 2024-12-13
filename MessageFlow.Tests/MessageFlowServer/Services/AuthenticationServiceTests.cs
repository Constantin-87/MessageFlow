using Xunit;
using Microsoft.AspNetCore.Identity;
using MessageFlow.Models;
using System.Threading.Tasks;
using MessageFlow.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using MessageFlow.Tests.Helpers;

namespace MessageFlow.Tests.MessageFlowServer.Services
{
    public class AuthenticationServiceTests : IAsyncLifetime
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthenticationServiceTests()
        {
            // Create test DbContext
            _context = TestDbContextFactory.CreateTestDbContext("AuthenticationServiceTestsDb");

            // Initialize UserManager and SignInManager using TestHelper
            _userManager = TestHelper.CreateUserManager(_context);
            _signInManager = TestHelper.CreateSignInManager(_userManager);
        }

        public async Task InitializeAsync()
        {
            // Ensure the database is deleted and recreated for a clean state
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            // Seed database
            await TestDatabaseSeeder.Seed(_context);
        }

        public async Task DisposeAsync()
        {
            // Drop the database after tests
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var email = "admin@companya.com"; // Updated to match seeded data
            var password = "Admin@123";       // Updated to match seeded password
            var user = await _userManager.FindByEmailAsync(email);

            // Act
            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsFailed()
        {
            // Arrange
            var email = "admin@companya.com"; // Updated to match seeded data
            var password = "InvalidPassword";
            var user = await _userManager.FindByEmailAsync(email);

            // Act
            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task Login_LockedOutAccount_ReturnsLockedOut()
        {
            // Arrange
            var email = "manager@companya.com"; // Updated to match seeded data
            var password = "Manager@123";       // Updated to match seeded password
            var user = await _userManager.FindByEmailAsync(email);

            // Enable lockout for the user
            user.LockoutEnabled = true;
            await _userManager.UpdateAsync(user);

            // Simulate failed login attempts to trigger lockout
            for (int i = 0; i < 5; i++)
            {
                await _userManager.AccessFailedAsync(user);
            }

            // Reload the user to ensure the lockout state is updated
            user = await _userManager.FindByEmailAsync(email);

            // Act
            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);

            // Assert
            Assert.True(result.IsLockedOut);
        }

    }
}
