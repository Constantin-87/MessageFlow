using System.Net.Http.Headers;

namespace MessageFlow.Server.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public UserActivityMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _logger = logger;
            _next = next;
            _httpClient = httpClientFactory.CreateClient("IdentityAPI");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip for specific paths like Webhook or for Static files
            var path = context.Request.Path;
            if (path.StartsWithSegments("/_blazor") || path.StartsWithSegments("/api/webhook"))
            {
                await _next(context);
                return;
            }

            // If user is authenticated, call Identity API to update activity
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var token = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Substring("Bearer ".Length));

                    try
                    {
                        await _httpClient.PostAsync("api/auth/update-activity", null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to update user activity: {ex.Message}");
                    }
                }
            }
            await _next(context);
        }
    }
}