using MessageFlow.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MessageFlow.Client.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
        }

        public async Task<(bool Success, string RedirectUrl)> LoginAsync(LoginModel loginmodel)
        {
            var client = _httpClientFactory.CreateClient("IdentityAPI");
            var response = await client.PostAsJsonAsync("api/auth/login", loginmodel);

            if (!response.IsSuccessStatusCode)
                return (false, "/Accounts/Login");

            var result = await response.Content.ReadFromJsonAsync<JWTResponseModel>();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);


            // Notify authentication state change
            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                authProvider.NotifyAuthenticationStateChanged();
            }

            // ✅ Get the updated authentication state
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // ✅ Determine redirect URL based on user's role
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            string redirectUrl = roles.Contains("Agent") ? "/AgentWorkspace"
                               : roles.Contains("SuperAdmin") || roles.Contains("Admin") || roles.Contains("Manager") ? "/AgentManagerWorkspace"
                               : "/";

            return (true, redirectUrl);
        }

        public async Task LogoutAsync()
        {
            var client = _httpClientFactory.CreateClient("IdentityAPI");

            await client.PostAsync("api/auth/logout", null);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");

            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                authProvider.NotifyAuthenticationStateChanged();
            }
        }

        public async Task<bool> TryRefreshTokenAsync()
        {
            var accessToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
                return false;

            var client = _httpClientFactory.CreateClient("IdentityAPI");

            var response = await client.PostAsJsonAsync("api/auth/refresh-token", new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<JWTResponseModel>();

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);

            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                authProvider.NotifyAuthenticationStateChanged();
            }

            return true;
        }

    }
}
