using MessageFlow.Client.Models.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace MessageFlow.Client.Services.Authentication
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly CurrentUserService _currentUser;
        private readonly ILogger<AuthService> _logger;
        private readonly UserHeartbeatService _heartbeat;

        private static readonly SemaphoreSlim _refreshLock = new(1, 1);
        private static Task<bool>? _refreshTask;

        public AuthService(
            IHttpClientFactory httpClientFactory,
            IJSRuntime jsRuntime,
            AuthenticationStateProvider authStateProvider,
            CurrentUserService currentUser,
            ILogger<AuthService> logger,
            UserHeartbeatService heartbeat)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
            _currentUser = currentUser;
            _logger = logger;
            _heartbeat = heartbeat;
        }

        public async Task<(bool Success, string RedirectUrl, string ErrorMessage)> LoginAsync(LoginDTO loginmodel)
        {
            var client = _httpClientFactory.CreateClient("IdentityAPI");
            var response = await client.PostAsJsonAsync("api/auth/login", loginmodel);

            var result = await response.Content.ReadFromJsonAsync<LoginResultDTO>();
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Locked)
                {
                    if (result?.LockoutEnd != null)
                    {
                        var unixLockout = ((DateTimeOffset)result.LockoutEnd).ToUnixTimeSeconds();
                        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "lockoutUntil", unixLockout.ToString());
                    }
                    return (false, "/Accounts/Lockout", result?.ErrorMessage ?? "Account is locked.");
                }

                return (false, "/Accounts/Login", result?.ErrorMessage ?? "Login failed.");
            }

            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", result.Token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken).AsTask();

            if (result?.User != null)
            {
                _currentUser.SetUser(result.User);
            }
            else
            {
                _logger.LogError("No user info returned from login response.");
            }

            // Notify authentication state change
            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                _ = Task.Run(() => authProvider.NotifyAuthenticationStateChanged());
            }

            var role = result.User.Role;

            string redirectUrl = role switch
            {
                "Agent" => "/AgentWorkspace",
                "SuperAdmin" or "Admin" or "AgentManager" => "/AgentManagerWorkspace",
                _ => "/"
            };

            // Delay heartbeat start to let token settle in storage
            _ = Task.Run(() => _heartbeat.Start());
            await Task.Delay(1000);
            return (true, redirectUrl, "");
        }

        public async Task LogoutAsync()
        {
            var client = _httpClientFactory.CreateClient("IdentityAPI");

            await client.PostAsync("api/auth/logout", null);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
            _currentUser.Clear();

            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                authProvider.NotifyAuthenticationStateChanged();
            }

            _heartbeat.Dispose();
        }

        public async Task<bool> TryRefreshTokenAsync()
        {
            await _refreshLock.WaitAsync();

            try
            {
                if (_refreshTask == null)
                {
                    _refreshTask = RefreshTokenInternalAsync();
                }

                return await _refreshTask;
            }
            catch (Exception)
            {
                _logger.LogError("Exception during token refresh.");
                return false;
            }
            finally
            {
                _refreshTask = null;
                _refreshLock.Release();
            }
        }

        private async Task<bool> RefreshTokenInternalAsync()
        {
            var accessToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");
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
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Token Refresh failed.");
                return false;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResultDTO>();
                if (result == null || string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.RefreshToken))
                {
                    return false;
                }
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", result.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while parsing refresh response.");
                return false;
            }

            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                await Task.Yield(); // Let rendering finish
                _ = Task.Run(() => authProvider.NotifyAuthenticationStateChanged());
            }

            return true;
        }
    }
}