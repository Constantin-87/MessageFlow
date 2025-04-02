using MessageFlow.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Security.Claims;

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

        public async Task<(bool Success, string RedirectUrl)> LoginAsync(LoginModel loginmodel)
        {
            _logger.LogInformation("Starting login process...");

            var client = _httpClientFactory.CreateClient("IdentityAPI");
            var response = await client.PostAsJsonAsync("api/auth/login", loginmodel);

            if (!response.IsSuccessStatusCode)
                return (false, "/Accounts/Login");

            _logger.LogInformation("Login succeeded. Reading response...");

            var result = await response.Content.ReadFromJsonAsync<JWTResponseModel>();

            _logger.LogInformation("Storing tokens in local storage...");


            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", result.Token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken).AsTask();

            if (result?.User != null)
            {
                _logger.LogInformation("Setting current user: {Username}", result.User.UserName);

                _currentUser.SetUser(result.User);
            }
            else
            {
                Console.WriteLine("No user info returned from login response.");
            }

            // Notify authentication state change
            if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
            {
                _logger.LogInformation("Triggering authentication state change...");
                //await Task.Delay(100);
                _ = Task.Run(() => authProvider.NotifyAuthenticationStateChanged());
            }

            //// ✅ Get the updated authentication state
            //var authState = await _authStateProvider.GetAuthenticationStateAsync();
            //var user = authState.User;

            var role = result.User.Role;
            _logger.LogInformation("User roles after login: {Roles}", role);

            string redirectUrl = role switch
            {
                "Agent" => "/AgentWorkspace",
                "SuperAdmin" or "Admin" or "Manager" => "/AgentManagerWorkspace",
                _ => "/"
            };


            _logger.LogInformation("Will redirect to: {RedirectUrl}", redirectUrl);

            //// ✅ Determine redirect URL based on user's role
            //var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();




            //string redirectUrl = roles.Contains("Agent") ? "/AgentWorkspace"
            //                   : roles.Contains("SuperAdmin") || roles.Contains("Admin") || roles.Contains("Manager") ? "/AgentManagerWorkspace"
            //                   : "/";



            // Delay heartbeat start to let token settle in storage
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("Delaying heartbeat start...");
                /*await Task.Delay(1000);*/ // small delay for localStorage propagation
                _logger.LogInformation("Starting heartbeat.");
                _heartbeat.Start();
            });
            await Task.Delay(1000);
            return (true, redirectUrl);
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
                else
                {
                    _logger.LogInformation("Waiting for ongoing refresh...");
                }

                return await _refreshTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during token refresh.");
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
                _logger.LogWarning($"Refresh failed. Status: {response.StatusCode}");
                return false;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<JWTResponseModel>();
                if (result == null || string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.RefreshToken))
                {
                    _logger.LogWarning("Failed to parse refresh response.");
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
