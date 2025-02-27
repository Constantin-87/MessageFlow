using MessageFlow.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MessageFlow.Server.Components.Accounts.Services
{
    public class UpdateLastActivityFilter : IActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UpdateLastActivityFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(context.HttpContext.User);
                var user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
                if (user != null)
                {
                    user.LastActivity = DateTime.UtcNow;
                    _userManager.UpdateAsync(user).GetAwaiter().GetResult();
                    // Debugging log
                    Console.WriteLine($"[UpdateLastActivityFilter] Updated LastActivity for userId: {userId} at {user.LastActivity}");
                }
                else
                {
                    Console.WriteLine($"[UpdateLastActivityFilter] User not found with userId: {userId}");
                }
            }
        }
    }

}
