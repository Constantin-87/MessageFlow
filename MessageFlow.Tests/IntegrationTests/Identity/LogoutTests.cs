using MessageFlow.Tests.Helpers;
using MessageFlow.Tests.Helpers.Factories;
using System.Net;

namespace MessageFlow.Tests.IntegrationTests.Identity
{
    public class LogoutTests : IClassFixture<IdentityApiFactory>
    {
        private readonly IdentityApiFactory _factory;

        public LogoutTests(IdentityApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Logout_WithValidToken_Succeeds()
        {
            var client = _factory.CreateClientWithTokenForRole("SuperAdmin", "1");
            var response = await client.PostAsync("/api/auth/logout", null);
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Logout_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = null;
            var response = await client.PostAsync("/api/auth/logout", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_WithInvalidUserId_ReturnsBadRequest()
        {
            var token = JwtTestHelper.GenerateTestJwt("Admin", userId: "non-existent-user");
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync("/api/auth/logout", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
