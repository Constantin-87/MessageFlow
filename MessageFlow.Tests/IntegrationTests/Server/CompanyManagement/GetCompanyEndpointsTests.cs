using System.Net.Http.Json;
using System.Net;
using MessageFlow.Shared.DTOs;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Server.CompanyManagement
{
    public class GetCompanyEndpointsTests : IClassFixture<ServerApiFactory>
    {
        private readonly HttpClient _client;

        public GetCompanyEndpointsTests(ServerApiFactory factory)
        {
            _client = factory.CreateClientWithSuperAdminAuth();
        }

        [Fact]
        public async Task GetAllCompanies_ReturnsSuccess()
        {
            var response = await _client.GetAsync("/api/company/all");

            Assert.True(response.IsSuccessStatusCode);
            var companies = await response.Content.ReadFromJsonAsync<List<CompanyDTO>>();
            Assert.NotNull(companies);
        }

        [Fact]
        public async Task GetCompanyById_ReturnsNotFound_ForInvalidId()
        {
            var response = await _client.GetAsync("/api/company/invalid-id");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCompanyForUser_ReturnsSuccess()
        {
            var response = await _client.GetAsync("/api/company/user-company");

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task GetCompanyMetadata_ReturnsBadRequest_ForInvalidId()
        {
            var response = await _client.GetAsync("/api/company/invalid-id/metadata");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task GetPretrainingFiles_ReturnsBadRequest_ForInvalidCompanyId()
        {
            var response = await _client.GetAsync("/api/company/invalid-id/pretraining-files");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
