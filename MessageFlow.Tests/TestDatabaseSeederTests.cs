using System.Linq;
using Xunit;
using MessageFlow.Data;

namespace MessageFlow.Tests
{
    public class TestDatabaseSeederTests
    {
        [Fact]
        public async Task TestSeeding()
        {
            // Arrange: Use a unique database name for isolation
            var dbName = "TestDatabaseSeederTestsDb";
            using var context = TestDbContextFactory.CreateTestDbContext(dbName);

            // Ensure the database is deleted before seeding to avoid conflicts
            await context.Database.EnsureDeletedAsync();

            // Act: Seed the database
            await TestDatabaseSeeder.Seed(context);

            // Assert: Verify seeded data exists
            Assert.True(context.Users.Any(u => u.UserName == "admin@companya.com"));
            Assert.True(context.Users.Any(u => u.UserName == "superadmin@headcompany.com"));
            Assert.True(context.Users.Any(u => u.UserName == "manager@companya.com"));
            Assert.True(context.Users.Any(u => u.UserName == "agent@companyb.com"));
            Assert.True(context.Users.Any(u => u.UserName == "agent@headcompany.com"));
            Assert.True(context.Companies.Any(c => c.CompanyName == "Company A")); // Match seeded name
            Assert.True(context.Companies.Any(c => c.CompanyName == "Company B")); // Match seeded name
            Assert.True(context.Companies.Any(c => c.CompanyName == "HeadCompany")); // Match new seeded name
            Assert.True(context.Teams.Any(t => t.TeamName == "Development Team")); // Match seeded team
            Assert.True(context.Teams.Any(t => t.TeamName == "Support Team"));     // Match seeded team
        }
    }
}
