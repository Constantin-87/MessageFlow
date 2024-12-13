using MessageFlow.Data;
using MessageFlow.Models;
using MessageFlow.Components.Channels.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MessageFlow.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Tests.MessageFlowServer.Services
{
    public class CompanyManagementServiceTests : IAsyncLifetime
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private CompanyManagementService _companyManagementServiceAdmin;
        private CompanyManagementService _companyManagementServiceSuperAdmin;

        public CompanyManagementServiceTests()
        {
            _dbContextFactory = TestDbContextFactory.CreateTestDbContextFactory();
        }

        public async Task InitializeAsync()
        {
            var dbContext = _dbContextFactory.CreateDbContext();

            // Ensure the database is deleted and recreated for a clean state
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            // Seed database
            await TestDatabaseSeeder.Seed(dbContext);

            // Initialize services with fresh DbContext instances
            _companyManagementServiceAdmin = TestHelper.CreateCompanyManagementService(_dbContextFactory, "3", "Admin");
            _companyManagementServiceSuperAdmin = TestHelper.CreateCompanyManagementService(_dbContextFactory, "1", "SuperAdmin");
        }

        public Task DisposeAsync()
        {
            // No explicit disposal needed; let the context factory manage it
            return Task.CompletedTask;
        }


        [Fact]
        public async Task SuperAdmin_Can_Create_Company()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var newCompany = new Company
            {
                CompanyName = "NewCompany",
                AccountNumber = "NEW-123"
            };

            // Act
            var result = await _companyManagementServiceSuperAdmin.CreateCompanyAsync(newCompany);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Company created successfully", result.errorMessage);
            Assert.NotNull(await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == "NewCompany"));
        }

        [Fact]
        public async Task Admin_Cannot_Create_Company()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var newCompany = new Company
            {
                CompanyName = "NewCompany",
                AccountNumber = "NEW-123"
            };

            // Act
            var result = await _companyManagementServiceAdmin.CreateCompanyAsync(newCompany);

            // Assert
            Assert.False(result.success);
            Assert.Equal("Only SuperAdmins can create companies.", result.errorMessage);
            Assert.Null(await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == "NewCompany"));
        }

        [Fact]
        public async Task SuperAdmin_Can_Delete_Company()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var companyToDelete = await dbContext.Companies.FirstAsync(c => c.CompanyName == "Company A");

            // Act
            var result = await _companyManagementServiceSuperAdmin.DeleteCompanyAsync(companyToDelete.Id);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Company and all associated data deleted successfully.", result.errorMessage);
            Assert.Null(await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyToDelete.Id));
        }

        [Fact]
        public async Task Admin_Cannot_Delete_Company()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var companyToDelete = await dbContext.Companies.FirstAsync(c => c.CompanyName == "Company A");

            // Act
            var result = await _companyManagementServiceAdmin.DeleteCompanyAsync(companyToDelete.Id);

            // Assert
            Assert.False(result.success);
            Assert.Equal("Only SuperAdmins can delete companies.", result.errorMessage);
            Assert.NotNull(await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyToDelete.Id));
        }

        [Fact]
        public async Task Admin_Can_Update_Own_Company_Details()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var adminCompany = await dbContext.Companies.FirstAsync(c => c.CompanyName == "Company A");
            adminCompany.CompanyName = "Updated Company A";

            // Act
            var result = await _companyManagementServiceAdmin.UpdateCompanyAsync(adminCompany);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Company updated successfully", result.errorMessage);

            var updatedCompany = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == adminCompany.Id);
            Assert.Equal("Updated Company A", updatedCompany.CompanyName);
        }

        [Fact]
        public async Task Admin_Cannot_Update_Other_Company_Details()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var otherCompany = await dbContext.Companies.FirstAsync(c => c.CompanyName == "Company B");

            // Detach the entity to prevent tracking
            dbContext.Entry(otherCompany).State = EntityState.Detached;

            otherCompany.CompanyName = "Updated Company B";

            // Act
            var result = await _companyManagementServiceAdmin.UpdateCompanyAsync(otherCompany);

            // Assert
            Assert.False(result.success);
            Assert.Equal("You are not authorized to update this company.", result.errorMessage);

            var unchangedCompany = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == otherCompany.Id);
            Assert.NotEqual("Updated Company B", unchangedCompany.CompanyName);
        }

        [Fact]
        public async Task SuperAdmin_Can_Update_Own_Company_Details()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var superAdminCompany = await dbContext.Companies.FirstAsync(c => c.CompanyName == "HeadCompany");
            superAdminCompany.CompanyName = "Updated HeadCompany";

            // Act
            var result = await _companyManagementServiceSuperAdmin.UpdateCompanyAsync(superAdminCompany);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Company updated successfully", result.errorMessage);

            var updatedCompany = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == superAdminCompany.Id);
            Assert.Equal("Updated HeadCompany", updatedCompany.CompanyName);
        }

        [Fact]
        public async Task SuperAdmin_Can_Update_Other_Company_Details()
        {
            var dbContext = _dbContextFactory.CreateDbContext();
            // Arrange
            var otherCompany = await dbContext.Companies.FirstAsync(c => c.CompanyName == "Company B");
            otherCompany.CompanyName = "Updated Company B";

            // Act
            var result = await _companyManagementServiceSuperAdmin.UpdateCompanyAsync(otherCompany);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Company updated successfully", result.errorMessage);

            var updatedCompany = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == otherCompany.Id);
            Assert.Equal("Updated Company B", updatedCompany.CompanyName);
        }

    }
}
