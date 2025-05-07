using System.Net;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Server.CompanyManagement
{
    public class CompanySearchTests : IClassFixture<ServerApiFactory>
    {
        private readonly HttpClient _authenticatedClient;
        private readonly HttpClient _unauthenticatedClient;

        public CompanySearchTests(ServerApiFactory factory)
        {
            _authenticatedClient = factory.CreateClientWithSuperAdminAuth();
            _unauthenticatedClient = factory.CreateUnauthenticatedClient();
        }

        [Fact]
        public async Task CreateSearchIndex_ReturnsOk_WhenValidCompanyId()
        {
            var companyId = "1";

            var response = await _authenticatedClient.PostAsync($"/api/company/{companyId}/create-search-index", null);

            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Index created and populated successfully.", content);
        }

        [Fact]
        public async Task CreateSearchIndex_ReturnsBadRequest_WhenInvalidCompanyId()
        {
            var companyId = "non-existent-company-id";

            var response = await _authenticatedClient.PostAsync($"/api/company/{companyId}/create-search-index", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("No processed data found for this company.", content);
        }

        [Fact]
        public async Task CreateSearchIndex_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            var companyId = "existing-company-id";

            var response = await _unauthenticatedClient.PostAsync($"/api/company/{companyId}/create-search-index", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateSearchIndex_ReturnsBadRequest_WhenCompanyIdMissing()
        {
            var companyId = "non-existing-company-id";

            var response = await _authenticatedClient.PostAsync($"/api/company/{companyId}/create-search-index", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}