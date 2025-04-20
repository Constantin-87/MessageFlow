using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop;
using Microsoft.IdentityModel.Tokens;
using MessageFlow.Client.Models.DTOs;

namespace MessageFlow.Client.Services.Authentication
{
    public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILogger<PersistentAuthenticationStateProvider> _logger;

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
                var token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return await DefaultUnauthenticatedTask;
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Check if the token is expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                    return await DefaultUnauthenticatedTask;
                }

                var claims = jwtToken.Claims.ToList();

                // Extract roles properly
                var roles = claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                // Set the current user
                if (!_currentUser.IsLoggedIn)
                {
                    _currentUser.SetUser(new ApplicationUserDTO
                    {
                        Id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "",
                        UserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "",
                        CompanyId = claims.FirstOrDefault(c => c.Type == "CompanyId")?.Value ?? "",
                        Role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "",
                        LockoutEnabled = bool.TryParse(claims.FirstOrDefault(c => c.Type == "LockoutEnabled")?.Value, out var locked) && locked, // or true, if you add this claim
                        LastActivity = DateTime.TryParse(claims.FirstOrDefault(c => c.Type == "LastActivity")?.Value, out var last) ? last : DateTime.UtcNow,
                        PhoneNumber = "",
                        UserEmail = ""
                    });
                }

                return new AuthenticationState(user);
            }
            catch (SecurityTokenException)
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                return await DefaultUnauthenticatedTask;
            }
            catch (Exception ex)
            {
                return await DefaultUnauthenticatedTask;
            }
        }

        // Method to trigger authentication state update when token is refreshed
        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
