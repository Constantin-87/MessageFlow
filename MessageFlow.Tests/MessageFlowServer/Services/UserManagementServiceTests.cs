using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.Tests.Helpers;

namespace MessageFlow.Tests.MessageFlowServer.Services
{
    public class UserManagementServiceTests : IAsyncLifetime
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManagementService _userManagementServiceAdmin;
        private readonly UserManagementService _userManagementServiceSuperAdmin;

        public UserManagementServiceTests()
        {
            _dbContext = TestDbContextFactory.CreateTestDbContext();
            _dbContextFactory = TestDbContextFactory.CreateTestDbContextFactory();
            // Initialize UserManager and RoleManager using TestHelper
            _userManager = TestHelper.CreateUserManager(_dbContext);
            _roleManager = TestHelper.CreateRoleManager(_dbContext);

            // Initialize UserManagementService for Admin and SuperAdmin using TestHelper
            _userManagementServiceAdmin = TestHelper.CreateUserManagementService(_dbContextFactory, _dbContext, _userManager, _roleManager, "3", "Admin");
            _userManagementServiceSuperAdmin = TestHelper.CreateUserManagementService(_dbContextFactory, _dbContext, _userManager, _roleManager, "1", "SuperAdmin");
        }

        public async Task InitializeAsync()
        {
            // Ensure the database is deleted and recreated for a clean state
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.Database.EnsureCreatedAsync();
            // Seed the database
            await TestDatabaseSeeder.Seed(_dbContext);
        }

        public async Task DisposeAsync()
        {
            // Drop the database after tests
            await _dbContext.Database.EnsureDeletedAsync();
        }

        [Fact]
        public async Task Admin_Creates_User_In_Same_Company_Should_Succeed()
        {
            // Arrange: Use the admin context
            var user = new ApplicationUser
            {
                UserName = "user1@test.com",
                Email = "user1@test.com",
                CompanyId = 2 // Same company as admin
            };
            var password = "Password123!";
            var role = "Agent";

            // Act
            var result = await _userManagementServiceAdmin.CreateUserAsync(user, password, role);

            // Assert
            Assert.True(result.success);
            Assert.Equal("User created successfully", result.errorMessage);

            var createdUser = await _userManager.FindByEmailAsync(user.Email);
            Assert.NotNull(createdUser);
            Assert.Contains(role, await _userManager.GetRolesAsync(createdUser));
            Assert.Equal(user.CompanyId, createdUser.CompanyId);
        }

        [Fact]
        public async Task Admin_Cannot_Create_User_In_Other_Company_Should_Fail()
        {
            // Arrange: Use the admin context for a different company
            var user = new ApplicationUser
            {
                UserName = "user2@test.com",
                Email = "user2@test.com",
                CompanyId = 3 // Different company from admin
            };
            var password = "Password123!";
            var role = "Agent";

            // Act
            var result = await _userManagementServiceAdmin.CreateUserAsync(user, password, role);

            // Assert
            Assert.False(result.success);
            Assert.Equal("You cannot create users for other companies.", result.errorMessage);
        }

        [Fact]
        public async Task SuperAdmin_Can_Create_User_In_Any_Company_Should_Succeed()
        {
            // Arrange: Use the super admin context
            var user = new ApplicationUser
            {
                UserName = "superuser@test.com",
                Email = "superuser@test.com",
                CompanyId = 2 // Any company, SuperAdmin can manage all
            };
            var password = "Password123!";
            var role = "Admin";

            // Act
            var result = await _userManagementServiceSuperAdmin.CreateUserAsync(user, password, role);

            // Assert
            Assert.True(result.success);
            Assert.Equal("User created successfully", result.errorMessage);

            var createdUser = await _userManager.FindByEmailAsync(user.Email);
            Assert.NotNull(createdUser);
            Assert.Contains(role, await _userManager.GetRolesAsync(createdUser));
            Assert.Equal(user.CompanyId, createdUser.CompanyId);
        }

        [Fact]
        public async Task Admin_Cannot_Assign_SuperAdmin_Role_Should_Fail()
        {
            // Arrange: Use the admin context
            var user = new ApplicationUser
            {
                UserName = "admin2@test.com",
                Email = "admin2@test.com",
                CompanyId = 2 // Same company as admin
            };
            var password = "Password123!";
            var role = "SuperAdmin"; // Invalid role for Admin

            // Act
            var result = await _userManagementServiceAdmin.CreateUserAsync(user, password, role);

            // Assert
            Assert.False(result.success);
            Assert.Equal("Only SuperAdmins can assign the SuperAdmin role.", result.errorMessage);
        }

        [Fact]
        public async Task SuperAdmin_Can_Assign_SuperAdmin_Role_Should_Succeed()
        {
            // Arrange: Use the super admin context
            var user = new ApplicationUser
            {
                UserName = "superadmin2@test.com",
                Email = "superadmin2@test.com",
                CompanyId = 1 // Head Company
            };
            var password = "Password123!";
            var role = "SuperAdmin"; // Valid role for SuperAdmin

            // Act
            var result = await _userManagementServiceSuperAdmin.CreateUserAsync(user, password, role);

            // Assert
            Assert.True(result.success);
            Assert.Equal("User created successfully", result.errorMessage);

            var createdUser = await _userManager.FindByEmailAsync(user.Email);
            Assert.NotNull(createdUser);
            Assert.Contains(role, await _userManager.GetRolesAsync(createdUser));
        }
    }
}
