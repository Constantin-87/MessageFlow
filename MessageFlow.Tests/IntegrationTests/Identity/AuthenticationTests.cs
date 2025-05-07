using System.Net.Http.Json;
using System.Net;
using MessageFlow.Identity.Models;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.IntegrationTests.Identity
{
    public class AuthenticationTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;

        public AuthenticationTests(IdentityApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                Username = "admin@companya.com",
                Password = "Admin@123"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LoginResultDTO>();

            Assert.False(string.IsNullOrEmpty(result?.Token));
            Assert.NotNull(result?.User);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                Username = "admin@company.com",
                Password = "WrongPassword"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
