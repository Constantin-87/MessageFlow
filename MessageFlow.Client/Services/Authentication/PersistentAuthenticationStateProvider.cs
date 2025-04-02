using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop;
using Microsoft.IdentityModel.Tokens;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Client.Services.Authentication
{
    public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILogger<PersistentAuthenticationStateProvider> _logger; // Add logger

        private readonly IJSRuntime _jsRuntime;
        private readonly CurrentUserService _currentUser;
        private static readonly Task<AuthenticationState> DefaultUnauthenticatedTask =
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        public PersistentAuthenticationStateProvider(ILogger<PersistentAuthenticationStateProvider> logger, IJSRuntime jsRuntime, CurrentUserService currentUser)
        {
            _logger = logger;
            _jsRuntime = jsRuntime;
            _currentUser = currentUser;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Retrieve JWT token from session storage
                _logger.LogInformation("Fetching auth token from session storage...");
                var token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token not found or empty.");
                    return await DefaultUnauthenticatedTask;
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                _logger.LogInformation("Token expiry (UTC): {Expiry}, Now (UTC): {Now}", jwtToken.ValidTo, DateTime.UtcNow);

                // Check if the token is expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token expired. Removing...");
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                    return await DefaultUnauthenticatedTask;
                }

                var claims = jwtToken.Claims.ToList();

                // Extract roles properly (checking different possible claim types)
                var roles = claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                _logger.LogInformation("Token valid. User: {User}, Roles: {Roles}",
                    claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                    string.Join(", ", roles));

                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                // Set the current user here from claims
                if (!_currentUser.IsLoggedIn)
                {
                    _logger.LogInformation("Setting current user in CurrentUserService.");

                    _currentUser.SetUser(new ApplicationUserDTO
                    {
                        Id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "",
                        UserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "",
                        CompanyId = claims.FirstOrDefault(c => c.Type == "CompanyId")?.Value ?? "",
                        Role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "",
                        LockoutEnabled = bool.TryParse(claims.FirstOrDefault(c => c.Type == "LockoutEnabled")?.Value, out var locked) && locked, // or true, if you add this claim
                        LastActivity = DateTime.TryParse(claims.FirstOrDefault(c => c.Type == "LastActivity")?.Value, out var last) ? last : DateTime.UtcNow,
                        PhoneNumber = "",       // not available in token, keep empty for now, fetch via IdentityAPI if needed
                        UserEmail = ""
                    });
                }

                return new AuthenticationState(user);
            }
            catch (SecurityTokenException)
            {
                _logger.LogWarning("Invalid token detected. Removing...");

                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                return await DefaultUnauthenticatedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while parsing token.");
                return await DefaultUnauthenticatedTask;
            }
        }

        // Method to trigger authentication state update when token is refreshed
        public void NotifyAuthenticationStateChanged()
        {
            _logger.LogInformation("NotifyAuthenticationStateChanged called.");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
