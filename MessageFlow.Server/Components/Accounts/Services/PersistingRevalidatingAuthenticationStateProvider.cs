//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.AspNetCore.Components.Server;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Options;
//using System.Diagnostics;
//using System.Security.Claims;
//using MessageFlow.Client;
//using MessageFlow.DataAccess.Models;
//using MessageFlow.Shared.DTOs;

//namespace MessageFlow.Server.Components.Accounts.Services
//{
//    // This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
//    // every 15 minutes an interactive circuit is connected. It also uses PersistentComponentState to flow the
//    // authentication state to the client which is then fixed for the lifetime of the WebAssembly application.
//    internal sealed class PersistingRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
//    {
//        private readonly IServiceScopeFactory scopeFactory;
//        private readonly PersistentComponentState state;
//        private readonly IdentityOptions options;

//        private readonly PersistingComponentStateSubscription subscription;

//        private Task<AuthenticationState>? authenticationStateTask;

//        public PersistingRevalidatingAuthenticationStateProvider(
//            ILoggerFactory loggerFactory,
//            IServiceScopeFactory serviceScopeFactory,
//            PersistentComponentState persistentComponentState,
//            IOptions<IdentityOptions> optionsAccessor)
//            : base(loggerFactory)
//        {
//            scopeFactory = serviceScopeFactory;
//            state = persistentComponentState;
//            options = optionsAccessor.Value;

//            AuthenticationStateChanged += OnAuthenticationStateChanged;
//            subscription = state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
//        }

//        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(15);

//        protected override async Task<bool> ValidateAuthenticationStateAsync(
//            AuthenticationState authenticationState, CancellationToken cancellationToken)
//        {
//            // Get the user manager from a new scope to ensure it fetches fresh data
//            await using var scope = scopeFactory.CreateAsyncScope();
//            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//            return await ValidateSecurityStampAsync(userManager, authenticationState.User);
//        }

//        private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
//        {
//            var user = await userManager.GetUserAsync(principal);
//            if (user is null)
//            {
//                return false;
//            }
//            else if (!userManager.SupportsUserSecurityStamp)
//            {
//                return true;
//            }
//            else
//            {
//                var principalStamp = principal.FindFirstValue(options.ClaimsIdentity.SecurityStampClaimType);
//                var userStamp = await userManager.GetSecurityStampAsync(user);
//                return principalStamp == userStamp;
//            }
//        }

//        private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
//        {
//            authenticationStateTask = task;
//        }

//        private async Task OnPersistingAsync()
//        {
//            if (authenticationStateTask is null)
//            {
//                throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");
//            }

//            var authenticationState = await authenticationStateTask;
//            var principal = authenticationState.User;

//            if (principal.Identity?.IsAuthenticated == true)
//            {
//                var userDto = new ApplicationUserDTO
//                {
//                    Id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
//                    UserName = principal.FindFirstValue(ClaimTypes.Name) ?? "",
//                    UserEmail = principal.FindFirstValue(ClaimTypes.Email) ?? "",
//                    PhoneNumber = principal.FindFirstValue(ClaimTypes.MobilePhone) ?? "",
//                    CompanyId = principal.FindFirstValue("CompanyId") ?? "",
//                    Role = principal.FindFirstValue(ClaimTypes.Role) ?? "",
//                    LockoutEnabled = bool.TryParse(principal.FindFirstValue("LockoutEnabled"), out var lockout) && lockout,
//                    LastActivity = DateTime.UtcNow
//                };

//                state.PersistAsJson(nameof(ApplicationUserDTO), userDto);
//            }
//        }

//        protected override void Dispose(bool disposing)
//        {
//            subscription.Dispose();
//            AuthenticationStateChanged -= OnAuthenticationStateChanged;
//            base.Dispose(disposing);
//        }

//    }
//}
