//using Microsoft.AspNetCore.Mvc.Filters;
//using System.Net.Http.Headers;
//using System.Security.Claims;

//namespace MessageFlow.Server.Middleware
//{
//    public class UpdateLastActivityFilter : IAsyncActionFilter
//    {
//        private readonly HttpClient _httpClient;

//        public UpdateLastActivityFilter(IHttpClientFactory httpClientFactory)
//        {
//            _httpClient = httpClientFactory.CreateClient("IdentityAPI");
//        }

//        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//        {
//            var resultContext = await next(); // Execute action first

//            var user = resultContext.HttpContext.User;
//            if (user.Identity?.IsAuthenticated == true)
//            {
//                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
//                if (!string.IsNullOrEmpty(userId))
//                {
//                    // 🔹 Retrieve the token from the request headers (if applicable)
//                    var token = resultContext.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

//                    if (!string.IsNullOrEmpty(token))
//                    {
//                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//                    }

//                    // 🔹 Call Identity API to update last activity
//                    var response = await _httpClient.PostAsync("api/auth/update-activity", null);

//                    if (response.IsSuccessStatusCode)
//                    {
//                        Console.WriteLine($"✅ [UpdateLastActivityFilter] Updated LastActivity for userId: {userId}");
//                    }
//                    else
//                    {
//                        var errorMessage = await response.Content.ReadAsStringAsync();
//                        Console.WriteLine($"❌ [UpdateLastActivityFilter] Failed to update last activity. Status: {response.StatusCode}, Error: {errorMessage}");
//                    }
//                }
//                else
//                {
//                    Console.WriteLine("[UpdateLastActivityFilter] UserId claim not found.");
//                }
//            }
//        }
//    }
//}
