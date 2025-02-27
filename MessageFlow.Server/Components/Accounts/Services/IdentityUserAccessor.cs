using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Server.Components.Accounts.Services
{
    internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager)
    {
        public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                Console.WriteLine("[IdentityUserAccessor] User is null, redirecting to InvalidUser page.");
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }
            else
            {
                Console.WriteLine($"[IdentityUserAccessor] Retrieved user: {user.UserName}, ID: {user.Id}");
            }
            return user;
        }
    }
}
