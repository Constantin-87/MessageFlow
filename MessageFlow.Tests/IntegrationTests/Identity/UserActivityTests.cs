using MessageFlow.Tests.Helpers.Factories;
using System.Net;

namespace MessageFlow.Tests.IntegrationTests.Identity
{
    public class UserActivityTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;

        public UserActivityTests(IdentityApiFactory factory)
        {
            _client = factory.CreateClientWithTokenForRole("SuperAdmin", "1");
        }

        [Fact]
        public async Task UpdateActivity_ForLoggedInUser_Succeeds()
        {
            var response = await _client.PostAsync("/api/auth/update-activity", null);
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task UpdateActivity_WithoutToken_ReturnsUnauthorized()
        {
            var client = _client;
            client.DefaultRequestHeaders.Authorization = null;
            var response = await client.PostAsync("/api/auth/update-activity", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}