using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.Infrastructure.Mappings;

namespace MessageFlow.Tests.Tests
{
    public class TestDatabaseSeederTests
    {
        [Fact]
        public async Task TestSeeding()
        {
            // Arrange
            var dbName = "TestDatabaseSeederTestsDb";
            var context = TestDbContextFactory.CreateDbContext(dbName);
            var unitOfWork = TestDbContextFactory.CreateUnitOfWork(context);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            var mapper = mapperConfig.CreateMapper();

            // ✅ Use mock user manager (empty user list for setup)
            var userManager = TestDbContextFactory.CreateRealUserManager(context);

            // ✅ RoleManager remains the same
            var roleManager = TestDbContextFactory.CreateRoleManager(unitOfWork);

            await unitOfWork.Context.Database.EnsureDeletedAsync();
            await unitOfWork.Context.Database.EnsureCreatedAsync();

            // Act
            await TestDatabaseSeeder.Seed(unitOfWork, mapper, userManager, roleManager);

            // Assert
            Assert.True(context.Users.Any(u => u.UserName == "admin@companya.com"));
            Assert.True(context.Users.Any(u => u.UserName == "superadmin@headcompany.com"));
            Assert.True(context.Users.Any(u => u.UserName == "manager@companya.com"));
            Assert.True(context.Users.Any(u => u.UserName == "agent@companyb.com"));
            Assert.True(context.Users.Any(u => u.UserName == "agent@headcompany.com"));

            Assert.True(context.Companies.Any(c => c.CompanyName == "Company A"));
            Assert.True(context.Companies.Any(c => c.CompanyName == "Company B"));
            Assert.True(context.Companies.Any(c => c.CompanyName == "HeadCompany"));

            Assert.True(context.Teams.Any(t => t.TeamName == "HQ Dev Team"));
            Assert.True(context.Teams.Any(t => t.TeamName == "A Support Team"));
        }
    }
}