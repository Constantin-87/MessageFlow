using Microsoft.JSInterop;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace MessageFlow.Client.Services.Authentication
{
    public class AuthHttpHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<AuthHttpHandler> _logger;
        private readonly AuthService _authService;
        private readonly UserHeartbeatService _heartbeat;
        private readonly CurrentUserService _currentUser;
        private readonly SessionExpiredNotifier _sessionNotifier;

        public AuthHttpHandler(
            IJSRuntime jsRuntime,
            AuthenticationStateProvider authStateProvider,
            ILogger<AuthHttpHandler> logger,
            AuthService authService,
            UserHeartbeatService heartbeat,
            CurrentUserService currentUser,
            SessionExpiredNotifier sessionNotifier)
        {
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
            _logger = logger;
            _authService = authService;
            _heartbeat = heartbeat;
            _currentUser = currentUser;
            _sessionNotifier = sessionNotifier;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            var isLogin = path.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase);
            var isRefresh = path.EndsWith("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase);
            var isRefreshOrLogin = isLogin || isRefresh;
            var token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");                        
            if (!string.IsNullOrEmpty(token) && !isRefreshOrLogin)
            {
                // Check expiration BEFORE sending the request
                if (IsTokenExpiring(token))
                {
                    var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
                    var refreshed = await _authService.TryRefreshTokenAsync();
                    if (refreshed)
                    {
                        token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed. Removing tokens and logging out.");
                        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
                        if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
                        {
                            authProvider.NotifyAuthenticationStateChanged();
                        }
                        _sessionNotifier.Trigger();
                    }
                }
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else if (string.IsNullOrEmpty(token) && !isRefreshOrLogin)
            {
                _logger.LogWarning("No authentication token found in local storage.");
            }
            var response = await base.SendAsync(request, cancellationToken);
            // If we get a 401, logout the user
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !isLogin)
            {
                _logger.LogWarning("Unauthorized request. Removing token and triggering logout.");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
                if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
                {
                    authProvider.NotifyAuthenticationStateChanged();
                }
            }
            if (response.IsSuccessStatusCode && _currentUser.IsLoggedIn)
            {
                var isSelfUpdateOrRefresh =
                    path.EndsWith("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("/api/auth/getCurrentUser", StringComparison.OrdinalIgnoreCase);

                if (!isSelfUpdateOrRefresh)
                {
                    await _heartbeat.ForceRefreshAsync();
                }
            }
            return response;
        }

        private bool IsTokenExpiring(string token)
        {
            var jwtHandler = new JwtSecurityTokenHandler();

            if (!jwtHandler.CanReadToken(token))
            {
                _logger.LogWarning("Cannot read JWT token.");
                return false;
            }

            var jwtToken = jwtHandler.ReadJwtToken(token);
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");

            if (expClaim == null)
            {
                _logger.LogWarning("No exp claim found in token.");
                return false;
            }

            var expUnix = long.Parse(expClaim.Value);
            var expiryDateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix);

            // Trigger refresh if less than 2 minutes remain
            var timeRemaining = expiryDateTimeUtc - DateTimeOffset.Now;

            return timeRemaining.TotalMinutes <= 2;
        }
    }
}