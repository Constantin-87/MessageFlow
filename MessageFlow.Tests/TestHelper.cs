using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MessageFlow.Components.Channels.Services;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Tests.Helpers
{
    public static class TestHelper
    {
        // Create a mocked UserManager
        public static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
        {
            var userStore = new UserStore<ApplicationUser>(context);
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());

            var userManager = new UserManager<ApplicationUser>(
                userStore,
                optionsMock.Object,
                new PasswordHasher<ApplicationUser>(),
                null, null, null, null,
                null,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );

            return userManager;
        }

        // Create a mocked RoleManager
        public static RoleManager<IdentityRole> CreateRoleManager(ApplicationDbContext context)
        {
            var roleStore = new RoleStore<IdentityRole>(context);
            return new RoleManager<IdentityRole>(
                roleStore,
                null, null, null,
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object
            );
        }

        // Create a mocked SignInManager
        public static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
        {
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var authenticationServiceMock = new Mock<IAuthenticationService>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationServiceMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;
            contextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);

            // Create valid IdentityOptions
            var identityOptions = new IdentityOptions();
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(identityOptions);

            var userPrincipalFactory = new UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsMock.Object);

            return new SignInManager<ApplicationUser>(
                userManager,
                contextAccessorMock.Object,
                userPrincipalFactory,
                null,
                new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
                null,
                null
            );
        }

        // Create a mocked IHttpContextAccessor with a specific user and role
        public static Mock<IHttpContextAccessor> CreateHttpContextAccessor(string userId, string role)
        {
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuthType"));

            httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);
            return httpContextAccessorMock;
        }

        // Create a CompanyManagementService with a mocked IHttpContextAccessor and ApplicationDbContext
        public static CompanyManagementService CreateCompanyManagementService(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            string userId,
            string role)
        {
            var loggerMock = new Mock<ILogger<CompanyManagementService>>();
            var httpContextAccessorMock = CreateHttpContextAccessor(userId, role);

            // Create a mock TeamsManagementService
            var teamsLoggerMock = new Mock<ILogger<TeamsManagementService>>();
            var teamsManagementService = new TeamsManagementService(dbContextFactory, teamsLoggerMock.Object);

            // Create and return the service using IDbContextFactory and TeamsManagementService
            return new CompanyManagementService(dbContextFactory, loggerMock.Object, httpContextAccessorMock.Object, teamsManagementService);
        }


        // Create a UserManagementService with a mocked IHttpContextAccessor, ApplicationDbContext, and other dependencies
        public static UserManagementService CreateUserManagementService(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            string userId,
            string role)
        {
            var loggerMock = new Mock<ILogger<UserManagementService>>();
            var teamsLoggerMock = new Mock<ILogger<TeamsManagementService>>();
            var httpContextAccessorMock = CreateHttpContextAccessor(userId, role);
            var teamsManagementService = new TeamsManagementService(dbContextFactory, teamsLoggerMock.Object);

            return new UserManagementService(
                userManager,
                new UserStore<ApplicationUser>(dbContext),
                roleManager,
                teamsManagementService,
                loggerMock.Object,
                httpContextAccessorMock.Object
            );
        }

        // Create a TeamsManagementService with a mocked ApplicationDbContext
        public static TeamsManagementService CreateTeamsManagementService(
            IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            var loggerMock = new Mock<ILogger<TeamsManagementService>>();
            return new TeamsManagementService(dbContextFactory, loggerMock.Object);
        }
    }
}
