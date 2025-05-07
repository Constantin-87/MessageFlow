using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Tests.Helpers.Factories;
using MessageFlow.Tests.Helpers;

namespace MessageFlow.Tests.UnitTests
{
    public class TestDatabaseSeederTests
    {
        [Fact]
        public async Task TestSeeding()
        {
            // Arrange
            var dbName = "TestDatabaseSeederTestsDb";
            var context = UnitTestFactory.CreateInMemoryDbContext(dbName);
            var unitOfWork = UnitTestFactory.CreateUnitOfWork(context);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            var mapper = mapperConfig.CreateMapper();

            // Use mock user manager
            var userManager = UnitTestFactory.CreateRealUserManager(context);

            // RoleManager remains the same
            var roleManager = UnitTestFactory.CreateRoleManager(context);

            await unitOfWork.Context.Database.EnsureDeletedAsync();
            await unitOfWork.Context.Database.EnsureCreatedAsync();

            // Act
            await TestDataSeeder.SeedAsync(unitOfWork, mapper, userManager, roleManager);

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