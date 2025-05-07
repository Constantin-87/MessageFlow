using System.Net;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Server.CompanyManagement
{
    public class CompanyMetadataTests : IClassFixture<ServerApiFactory>
    {
        private readonly HttpClient _authenticatedClient;
        private readonly HttpClient _unauthenticatedClient;

        public CompanyMetadataTests(ServerApiFactory factory)
        {
            _authenticatedClient = factory.CreateClientWithSuperAdminAuth();
            _unauthenticatedClient = factory.CreateUnauthenticatedClient();
        }

        [Fact]
        public async Task GetMetadata_ReturnsOk_WhenCompanyExists()
        {
            var companyId = "1";

            var response = await _authenticatedClient.GetAsync($"/api/company/{companyId}/metadata");

            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content));
        }

        [Fact]
        public async Task GenerateMetadata_ReturnsOk_WhenValid()
        {
            var companyId = "1";

            var response = await _authenticatedClient.PostAsync($"/api/company/{companyId}/generate-metadata", null);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task DeleteMetadata_ReturnsOk_WhenValid()
        {
            var companyId = "1";

            var response = await _authenticatedClient.DeleteAsync($"/api/company/{companyId}/delete-metadata");

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task GetMetadata_ReturnsBadRequest_WhenInvalidCompanyId()
        {
            var companyId = "nonexistent-company-id";

            var response = await _authenticatedClient.GetAsync($"/api/company/{companyId}/metadata");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GenerateMetadata_ReturnsBadRequest_WhenCompanyDoesNotExist()
        {
            var companyId = "nonexistent-company-id";

            var response = await _authenticatedClient.PostAsync($"/api/company/{companyId}/generate-metadata", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMetadata_ReturnsBadRequest_WhenInvalidCompanyId()
        {
            var companyId = "nonexistent-company-id";

            var response = await _authenticatedClient.DeleteAsync($"/api/company/{companyId}/delete-metadata");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GenerateMetadata_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            var companyId = "1";

            var response = await _unauthenticatedClient.PostAsync($"/api/company/{companyId}/generate-metadata", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMetadata_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            var companyId = "1";

            var response = await _unauthenticatedClient.DeleteAsync($"/api/company/{companyId}/delete-metadata");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}