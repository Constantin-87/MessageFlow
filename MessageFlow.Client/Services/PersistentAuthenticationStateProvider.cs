using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop;
using Microsoft.IdentityModel.Tokens;

namespace MessageFlow.Client.Services
{
    public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private static readonly Task<AuthenticationState> DefaultUnauthenticatedTask =
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        public PersistentAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Retrieve JWT token from localStorage
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return await DefaultUnauthenticatedTask;
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Check if the token is expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                    return await DefaultUnauthenticatedTask;
                }

                var claims = jwtToken.Claims.ToList();

                // Extract roles properly (checking different possible claim types)
                var roles = claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (SecurityTokenException)
            {
                Console.WriteLine("Invalid token detected, removing...");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                return await DefaultUnauthenticatedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
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
