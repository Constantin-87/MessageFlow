using MessageFlow.DataAccess.Models;
using MessageFlow.Tests.Helpers;
using MessageFlow.Tests.Helpers.Factories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MessageFlow.Tests.IntegrationTests.Identity
{
    public class SessionTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;

        public SessionTests(IdentityApiFactory factory)
        {
            _client = factory.CreateClientWithTokenForRole("SuperAdmin", "1");
        }

        [Fact]
        public async Task ValidateSession_WithValidToken_ReturnsUserData()
        {
            var response = await _client.GetAsync("/api/auth/session");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("1", json.GetProperty("userId").GetString());
            Assert.Equal("superadmin@headcompany.com", json.GetProperty("username").GetString());
        }

        [Fact]
        public async Task ValidateSession_WithoutToken_ReturnsUnauthorized()
        {
            var client = new IdentityApiFactory().CreateClient();
            var response = await client.GetAsync("/api/auth/session");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ValidateSession_WithExpiredLastActivity_ReturnsUnauthorized()
        {
            var factory = new IdentityApiFactory();
            var token = JwtTestHelper.GenerateTestJwt("SuperAdmin", "1");
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync("1");
            user.LastActivity = DateTime.UtcNow.AddMinutes(-20);
            await userManager.UpdateAsync(user);

            var response = await client.GetAsync("/api/auth/session");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ValidateSession_WithNonExistentUser_ReturnsUnauthorized()
        {
            var token = JwtTestHelper.GenerateTestJwt("Admin", "ghost-user");
            var client = new IdentityApiFactory().CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/auth/session");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}