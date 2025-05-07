using MessageFlow.Tests.Helpers;
using MessageFlow.Shared.DTOs;
using System.Net;
using System.Net.Http.Json;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Identity
{
    public class CurrentUserTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;
        private readonly IdentityApiFactory _factory;

        public CurrentUserTests(IdentityApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClientWithTokenForRole("SuperAdmin", "1");
        }

        [Fact]
        public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
        {
            var response = await _client.GetAsync("/api/auth/getCurrentUser");
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<ApplicationUserDTO>();
            Assert.NotNull(user);
            Assert.Equal("superadmin@headcompany.com", user.UserName);
            Assert.Equal("HeadCompany", user.CompanyDTO?.CompanyName);
        }

        [Fact]
        public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/auth/getCurrentUser");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUser_WithInvalidUserId_ReturnsNotFound()
        {
            var token = JwtTestHelper.GenerateTestJwt("Admin", "non-existent-user");
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/auth/getCurrentUser");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUser_WithExpiredToken_ReturnsUnauthorized()
        {
            var expiredToken = JwtTestHelper.GenerateTestJwt("Admin", "test-user", DateTime.UtcNow.AddMinutes(-60));
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

            var response = await client.GetAsync("/api/auth/getCurrentUser");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
