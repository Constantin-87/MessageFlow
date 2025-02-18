using Xunit;
using MessageFlow.Server.Data;
using MessageFlow.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MessageFlow.Server.Components.Accounts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MessageFlow.Server.Tests.Helpers;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MessageFlow.Server.Tests.IntegrationTests
{
    public class SuperAdminEndToEndWorkflowTests : IAsyncLifetime
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly CompanyManagementService _companyManagementServiceSuperAdmin;
        private readonly UserManagementService _userManagementServiceSuperAdmin;
        private UserManagementService _userManagementServiceAdmin;

        public SuperAdminEndToEndWorkflowTests()
        {
            _dbContextFactory = TestDbContextFactory.CreateTestDbContextFactory();
            // Directly create ApplicationDbContext via TestDbContextFactory
            _dbContext = TestDbContextFactory.CreateTestDbContext();  // Create the context here

            // Initialize UserManager and SignInManager using TestHelper
            _userManager = TestHelper.CreateUserManager(_dbContext);
            _signInManager = TestHelper.CreateSignInManager(_userManager);

            // Initialize UserManagementService for SuperAdmin using TestHelper
            _userManagementServiceSuperAdmin = TestHelper.CreateUserManagementService(
                _dbContextFactory,
                _dbContext,
                _userManager,
                TestHelper.CreateRoleManager(_dbContext),
                "1",
                "SuperAdmin");

            // Initialize CompanyManagementService for SuperAdmin using TestHelper
            _companyManagementServiceSuperAdmin = TestHelper.CreateCompanyManagementService(
                _dbContextFactory,  // Pass the ApplicationDbContext directly
                "1",
                "SuperAdmin");
        }

        public async Task InitializeAsync()
        {
            // Ensure a clean database state
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.Database.EnsureCreatedAsync();
            await TestDatabaseSeeder.Seed(_dbContext);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.Database.EnsureDeletedAsync();
        }

        [Fact]
        public async Task IT01_SuperAdminEndToEndWorkflow()
        {
            // Step 1: SuperAdmin logs in
            var superAdminEmail = "superadmin@headcompany.com";
            var superAdminPassword = "SuperAdmin@123";
            var superAdminUser = await _userManager.FindByEmailAsync(superAdminEmail);
            Assert.NotNull(superAdminUser);
            var loginResult = await _signInManager.PasswordSignInAsync(superAdminUser, superAdminPassword, false, true);
            Assert.True(loginResult.Succeeded);

            // Step 2: SuperAdmin creates a new company
            var companyName = "Tech Innovators";
            var accountNumber = "TECH-123";
            var newCompany = new Company { CompanyName = companyName, AccountNumber = accountNumber };
            var companyResult = await _companyManagementServiceSuperAdmin.CreateCompanyAsync(newCompany);
            Assert.True(companyResult.success);

            // Step 3: SuperAdmin creates a new Admin for the company
            var adminEmail = "admin@techinnovators.com";
            var adminPassword = "Admin@123";
            var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, CompanyId = newCompany.Id };
            var adminResult = await _userManagementServiceSuperAdmin.CreateUserAsync(adminUser, adminPassword, "Admin");
            Assert.True(adminResult.success);

            // Dynamically set up UserManagementService for the newly created Admin
            var adminHttpContextAccessor = TestHelper.CreateHttpContextAccessor(adminUser.Id, "Admin");
            var roleManager = TestHelper.CreateRoleManager(_dbContext);
            var loggerUserManagement = new Mock<ILogger<UserManagementService>>();
            var teamsManagementService = new TeamsManagementService(_dbContextFactory, new Mock<ILogger<TeamsManagementService>>().Object);

            _userManagementServiceAdmin = new UserManagementService(
                _userManager,
                new UserStore<ApplicationUser>(_dbContext),
                roleManager,
                teamsManagementService,
                loggerUserManagement.Object,
                adminHttpContextAccessor.Object
            );

            // Step 4: Admin logs in
            var createdAdminUser = await _userManager.FindByEmailAsync(adminEmail);
            Assert.NotNull(createdAdminUser);
            loginResult = await _signInManager.PasswordSignInAsync(createdAdminUser, adminPassword, false, true);
            Assert.True(loginResult.Succeeded);

            // Step 5: Admin creates a Manager user
            var managerEmail = "manager@techinnovators.com";
            var managerPassword = "Manager@123";
            var managerUser = new ApplicationUser { UserName = managerEmail, Email = managerEmail, CompanyId = newCompany.Id };
            var managerResult = await _userManagementServiceAdmin.CreateUserAsync(managerUser, managerPassword, "Manager");
            Assert.True(managerResult.success);

            // Step 6: Admin creates a new team
            var teamName = "Development Team";
            var team = new Team { TeamName = teamName, CompanyId = newCompany.Id };
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Step 7: Admin assigns Manager to the team
            managerResult = await _userManagementServiceAdmin.UpdateUserAsync(managerUser, null, "Manager");
            Assert.True(managerResult.success);

            // Step 8: SuperAdmin updates Manager role to Admin
            var updateRoleResult = await _userManagementServiceSuperAdmin.UpdateUserAsync(managerUser, null, "Admin");
            Assert.True(updateRoleResult.success);

            // Step 9: SuperAdmin deletes the company
            var deleteCompanyResult = await _companyManagementServiceSuperAdmin.DeleteCompanyAsync(newCompany.Id);
            Assert.True(deleteCompanyResult.success);

            // Verify the company no longer exists
            Assert.Null(await _dbContext.Companies.FindAsync(newCompany.Id));

            // Verify no users associated with the company exist
            var deletedUsers = await _dbContext.Users.Where(u => u.CompanyId == newCompany.Id).ToListAsync();
            Assert.Empty(deletedUsers);

            // Verify no teams associated with the company exist
            var deletedTeams = await _dbContext.Teams.Where(t => t.CompanyId == newCompany.Id).ToListAsync();
            Assert.Empty(deletedTeams);

            // Verify no user roles for deleted users exist
            var userIds = deletedUsers.Select(u => u.Id).ToList();
            var deletedUserRoles = await _dbContext.UserRoles.Where(ur => userIds.Contains(ur.UserId)).ToListAsync();
            Assert.Empty(deletedUserRoles);

            // Verify no user claims for deleted users exist
            var deletedUserClaims = await _dbContext.UserClaims.Where(uc => userIds.Contains(uc.UserId)).ToListAsync();
            Assert.Empty(deletedUserClaims);

            // Verify no user logins for deleted users exist
            var deletedUserLogins = await _dbContext.UserLogins.Where(ul => userIds.Contains(ul.UserId)).ToListAsync();
            Assert.Empty(deletedUserLogins);

            // Verify no user tokens for deleted users exist
            var deletedUserTokens = await _dbContext.UserTokens.Where(ut => userIds.Contains(ut.UserId)).ToListAsync();
            Assert.Empty(deletedUserTokens);

            // Verify no user-team relationships exist
            var deletedUserTeams = await _dbContext.UserTeams.Where(ut => userIds.Contains(ut.UserId)).ToListAsync();
            Assert.Empty(deletedUserTeams);
        }
    }
}
