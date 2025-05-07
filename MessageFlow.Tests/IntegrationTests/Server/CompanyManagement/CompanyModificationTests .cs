using System.Net;
using System.Net.Http.Json;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Server.CompanyManagement
{
    public class CompanyModificationTests : IClassFixture<ServerApiFactory>
    {
        private readonly HttpClient _client;

        public CompanyModificationTests(ServerApiFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!roleManager.Roles.Any(r => r.Name == "SuperAdmin"))
                roleManager.CreateAsync(new IdentityRole("SuperAdmin")).Wait();

            var user = new ApplicationUser
            {
                Id = "test-user",
                UserName = "superadmin@test.com",
                Email = "superadmin@test.com",
                EmailConfirmed = true,
                CompanyId = "existing-company-id"
            };

            if (userManager.FindByIdAsync(user.Id).Result == null)
            {
                userManager.CreateAsync(user, "ValidPassword123!").Wait();
                userManager.AddToRoleAsync(user, "SuperAdmin").Wait();
            }

            _client = factory.CreateClientWithSuperAdminAuth();
        }

        [Fact]
        public async Task CreateCompany_ReturnsOk_WhenValid()
        {
            var company = new CompanyDTO
            {
                AccountNumber = "NEW-001",
                CompanyName = "Test Company",
                Description = "Test description",
                IndustryType = "Tech",
                WebsiteUrl = "https://testcompany.com"
            };

            var response = await _client.PostAsJsonAsync("/api/company/create", company);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task UpdateCompany_ReturnsOk_WhenValid()
        {
            // Get all companies to confirm the seeded company exists
            var getAllResponse = await _client.GetAsync("/api/company/all");
            var allContent = await getAllResponse.Content.ReadAsStringAsync();

            var companies = await getAllResponse.Content.ReadFromJsonAsync<List<CompanyDTO>>();
            var existingCompany = companies?.FirstOrDefault(c => c.Id == "1");

            Assert.NotNull(existingCompany);

            // Attempt update
            var updatedCompany = new CompanyDTO
            {
                Id = existingCompany.Id,
                AccountNumber = "UPDATED-123",
                CompanyName = "Updated Name",
                Description = "Changed desc",
                IndustryType = "Updated",
                WebsiteUrl = "https://changed.com"
            };

            var response = await _client.PutAsJsonAsync("/api/company/update", updatedCompany);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
        }


        [Fact]
        public async Task DeleteCompany_ReturnsBadRequest_ForInvalidId()
        {
            var response = await _client.DeleteAsync("/api/company/delete/nonexistent-id");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateCompany_ReturnsBadRequest_WhenMissingRequiredFields()
        {
            var incompleteCompany = new CompanyDTO
            {
                CompanyName = "",
                AccountNumber = null
            };

            var response = await _client.PostAsJsonAsync("/api/company/create", incompleteCompany);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCompany_ReturnsBadRequest_WhenCompanyDoesNotExist()
        {
            var nonExistentCompany = new CompanyDTO
            {
                Id = "nonexistent-id",
                AccountNumber = "X123",
                CompanyName = "Ghost Corp",
                Description = "Doesn't exist",
                IndustryType = "None",
                WebsiteUrl = "https://ghost.com"
            };

            var response = await _client.PutAsJsonAsync("/api/company/update", nonExistentCompany);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCompany_ReturnsOk_WhenValidId()
        {
            var company = new CompanyDTO
            {
                AccountNumber = "TO-DELETE-001",
                CompanyName = "Company To Delete",
                Description = "Temporary for deletion test",
                IndustryType = "Temp",
                WebsiteUrl = "https://tempdelete.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/company/create", company);
            Assert.True(createResponse.IsSuccessStatusCode);

            var getAllResponse = await _client.GetAsync("/api/company/all");
            var companies = await getAllResponse.Content.ReadFromJsonAsync<List<CompanyDTO>>();
            var createdCompany = companies?.FirstOrDefault(c => c.CompanyName == "Company To Delete");

            Assert.NotNull(createdCompany);
            Assert.False(string.IsNullOrEmpty(createdCompany.Id));

            var deleteResponse = await _client.DeleteAsync($"/api/company/delete/{createdCompany.Id}");

            var content = await deleteResponse.Content.ReadAsStringAsync();

            Assert.True(deleteResponse.IsSuccessStatusCode);
        }
    }
}