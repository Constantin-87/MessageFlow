using Microsoft.JSInterop;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace MessageFlow.Client.Services
{
    public class AuthHttpHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<AuthHttpHandler> _logger;

        public AuthHttpHandler(IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider, ILogger<AuthHttpHandler> logger)
        {
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("No authentication token found in local storage.");
            }

            var response = await base.SendAsync(request, cancellationToken);

            // If token expired, force logout
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized request. Removing token and triggering logout.");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");

                if (_authStateProvider is PersistentAuthenticationStateProvider authProvider)
                {
                    authProvider.NotifyAuthenticationStateChanged();
                }
            }

            return response;
        }
    }
}
