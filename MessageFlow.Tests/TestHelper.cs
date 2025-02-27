using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using MessageFlow.DataAccess.Services;
using MessageFlow.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using MessageFlow.Server.Components.Accounts.Services;
using MessageFlow.AzureServices.Services;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MessageFlow.Tests
{
    public static class TestHelper
    {
        // Create a mocked UserManager using IUnitOfWork
        public static UserManager<ApplicationUser> CreateUserManager(IUnitOfWork unitOfWork)
        {
            var userStore = new UserStore<ApplicationUser>(unitOfWork.Context);
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());

            return new UserManager<ApplicationUser>(
                userStore,
                optionsMock.Object,
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );
        }

        // Create a mocked RoleManager using IUnitOfWork
        public static RoleManager<IdentityRole> CreateRoleManager(IUnitOfWork unitOfWork)
        {
            var roleStore = new RoleStore<IdentityRole>(unitOfWork.Context);
            return new RoleManager<IdentityRole>(
                roleStore,
                Array.Empty<IRoleValidator<IdentityRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object
            );
        }

        // Create a mocked SignInManager using IUnitOfWork
        public static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
        {
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var serviceProviderMock = new Mock<IServiceProvider>();
            var authenticationServiceMock = new Mock<IAuthenticationService>();

            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationServiceMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;
            httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);

            return new SignInManager<ApplicationUser>(
                userManager,
                httpContextAccessorMock.Object,
                new UserClaimsPrincipalFactory<ApplicationUser>(userManager, new Mock<IOptions<IdentityOptions>>().Object),
                null,
                new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
                null,
                null
            );
        }

        // Create a mocked IHttpContextAccessor with user and role claims
        public static Mock<IHttpContextAccessor> CreateHttpContextAccessor(string userId, string role)
        {
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, role)
                }, "TestAuthType"))
            };

            httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);
            return httpContextAccessorMock;
        }

        // Create CompanyManagementService using IUnitOfWork, IMapper, and IHttpContextAccessor
        public static CompanyManagementService CreateCompanyManagementService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            string userId,
            string role)
        {
            var loggerMock = new Mock<ILogger<CompanyManagementService>>();
            var httpContextAccessorMock = CreateHttpContextAccessor(userId, role);

            var teamsService = new TeamsManagementService(unitOfWork, new Mock<ILogger<TeamsManagementService>>().Object, mapper);
            var azureSearchMock = new Mock<AzureSearchService>("https://fake-search-endpoint", "fake-api-key");
            var documentProcessingMock = new Mock<DocumentProcessingService>(new Mock<IConfiguration>().Object, new Mock<ILogger<DocumentProcessingService>>().Object);
            var blobStorageMock = new Mock<AzureBlobStorageService>(new Mock<IConfiguration>().Object);

            return new CompanyManagementService(
                unitOfWork,
                loggerMock.Object,
                httpContextAccessorMock.Object,
                teamsService,
                blobStorageMock.Object,
                documentProcessingMock.Object,
                azureSearchMock.Object,
                mapper
            );
        }

        // Create UserManagementService with IUnitOfWork and necessary mocks
        public static UserManagementService CreateUserManagementService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            string userId,
            string role)
        {
            var loggerMock = new Mock<ILogger<UserManagementService>>();
            var httpContextAccessorMock = CreateHttpContextAccessor(userId, role);

            var teamsService = new TeamsManagementService(unitOfWork, new Mock<ILogger<TeamsManagementService>>().Object, mapper);

            // ✅ Create UserStore using the context from UnitOfWork
            var userStore = new UserStore<ApplicationUser>(unitOfWork.Context);

            return new UserManagementService(
                unitOfWork,
                teamsService,
                loggerMock.Object,
                httpContextAccessorMock.Object,
                mapper
            );
        }

        //// Create TeamsManagementService with IUnitOfWork
        //public static TeamsManagementService CreateTeamsManagementService(IUnitOfWork unitOfWork)
        //{
        //    var loggerMock = new Mock<ILogger<TeamsManagementService>>();
        //    return new TeamsManagementService(unitOfWork, loggerMock.Object);
        //}
    }
}