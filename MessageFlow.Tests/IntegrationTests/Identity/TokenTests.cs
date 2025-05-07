using MessageFlow.Tests.Helpers.Factories;
using System.Net;
using System.Net.Http.Json;

namespace MessageFlow.Tests.IntegrationTests.Identity
{
    public class TokenTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;

        public TokenTests(IdentityApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RefreshToken_WithInvalidData_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
            {
                accessToken = "invalid",
                refreshToken = "invalid"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RevokeRefreshToken_WithToken_ReturnsSuccess()
        {
            var client = new IdentityApiFactory().CreateClientWithTokenForRole("SuperAdmin", "1");
            var response = await client.PostAsync("/api/auth/revoke-refresh-token", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}