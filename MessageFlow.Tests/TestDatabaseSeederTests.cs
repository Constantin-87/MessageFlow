using System.Linq;
using Xunit;
using MessageFlow.DataAccess.Configurations;
using AutoMapper;
using MessageFlow.Infrastructure.Mappings;

namespace MessageFlow.Tests
{
    public class TestDatabaseSeederTests
    {
        [Fact]
        public async Task TestSeeding()
        {
            // Arrange: Create the UnitOfWork, Mapper, UserManager, and RoleManager
            var dbName = "TestDatabaseSeederTestsDb";

            // ✅ Create ApplicationDbContext with the database name
            var context = TestDbContextFactory.CreateDbContext(dbName);

            // ✅ Pass the context to CreateUnitOfWork
            var unitOfWork = TestDbContextFactory.CreateUnitOfWork(context);

            // Create Mapper using a real profile (replace with your actual AutoMapper profile)
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();  // Replace with your actual profile class name
            });

            var mapper = mapperConfig.CreateMapper();

            var userManager = TestHelper.CreateUserManager(unitOfWork);
            var roleManager = TestHelper.CreateRoleManager(unitOfWork);

            // Ensure the database is clean before seeding
            await unitOfWork.Context.Database.EnsureDeletedAsync();
            await unitOfWork.Context.Database.EnsureCreatedAsync();

            // Act: Seed the database
            await TestDatabaseSeeder.Seed(unitOfWork, mapper, userManager, roleManager);

            Assert.True(context.Users.Any(u => u.UserName == "admin@companya.com"));
            Assert.True(context.Users.Any(u => u.UserName == "superadmin@headcompany.com"));
            Assert.True(context.Users.Any(u => u.UserName == "manager@companya.com"));
            Assert.True(context.Users.Any(u => u.UserName == "agent@companyb.com"));
            Assert.True(context.Users.Any(u => u.UserName == "agent@headcompany.com"));

            Assert.True(context.Companies.Any(c => c.CompanyName == "Company A"));
            Assert.True(context.Companies.Any(c => c.CompanyName == "Company B"));
            Assert.True(context.Companies.Any(c => c.CompanyName == "HeadCompany"));

            Assert.True(context.Teams.Any(t => t.TeamName == "Development Team"));
            Assert.True(context.Teams.Any(t => t.TeamName == "Support Team"));

            //// Arrange: Use a unique database name for isolation
            //var dbName = "TestDatabaseSeederTestsDb";
            //using var context = TestDbContextFactory.CreateTestDbContext(dbName);

            //// Ensure the database is deleted before seeding to avoid conflicts
            //await context.Database.EnsureDeletedAsync();

            //// Act: Seed the database
            //await TestDatabaseSeeder.Seed(context);

            //// Assert: Verify seeded data exists
            //Assert.True(context.Users.Any(u => u.UserName == "admin@companya.com"));
            //Assert.True(context.Users.Any(u => u.UserName == "superadmin@headcompany.com"));
            //Assert.True(context.Users.Any(u => u.UserName == "manager@companya.com"));
            //Assert.True(context.Users.Any(u => u.UserName == "agent@companyb.com"));
            //Assert.True(context.Users.Any(u => u.UserName == "agent@headcompany.com"));
            //Assert.True(context.Companies.Any(c => c.CompanyName == "Company A")); // Match seeded name
            //Assert.True(context.Companies.Any(c => c.CompanyName == "Company B")); // Match seeded name
            //Assert.True(context.Companies.Any(c => c.CompanyName == "HeadCompany")); // Match new seeded name
            //Assert.True(context.Teams.Any(t => t.TeamName == "Development Team")); // Match seeded team
            //Assert.True(context.Teams.Any(t => t.TeamName == "Support Team"));     // Match seeded team
        }
    }
}
