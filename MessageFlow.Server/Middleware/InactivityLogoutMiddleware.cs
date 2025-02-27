using MessageFlow.DataAccess.Models;
using Microsoft.AspNetCore.Identity;

namespace MessageFlow.Server.Middleware
{
    public class InactivityLogoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(15); // Inactivity timeout duration

        public InactivityLogoutMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<InactivityLogoutMiddleware> logger)
        {
            logger.LogInformation($"Processing request for path: {context.Request.Path}");

            // Log if bypass condition is met
            if (context.Request.Path.StartsWithSegments("/_blazor") ||
                context.Request.Path.StartsWithSegments("/api/webhook") ||
                context.Request.Path.StartsWithSegments("/chatHub"))
            {
                logger.LogInformation("Bypassing middleware for path: {Path}", context.Request.Path);
                await _next(context);
                return;
            }

            // Log authentication status
            if (context.User.Identity?.IsAuthenticated == true)
            {
                logger.LogInformation("User is authenticated. Checking for inactivity...");

                var userId = userManager.GetUserId(context.User);
                logger.LogInformation("Authenticated user ID: {UserId}", userId);

                var user = await userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    logger.LogInformation("User found. LastActivity: {LastActivity}, CurrentTime: {CurrentTime}", user.LastActivity, DateTime.UtcNow);

                    if (DateTime.UtcNow - user.LastActivity > _timeout)
                    {
                        logger.LogInformation("User session timed out. Logging out user ID: {UserId}", userId);
                        await signInManager.SignOutAsync();
                        logger.LogInformation("Redirecting to /Accounts/Login due to inactivity.");
                        context.Response.Redirect("/Accounts/Login");
                        return;
                    }
                }
                else
                {
                    logger.LogWarning("User with ID {UserId} not found in database.", userId);
                }
            }
            else
            {
                logger.LogInformation("User is not authenticated. Proceeding to next middleware.");
            }

            logger.LogInformation("Proceeding to the next middleware for path: {Path}", context.Request.Path);
            await _next(context);
        }


    }

}
