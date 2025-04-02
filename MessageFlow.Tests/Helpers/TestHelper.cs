using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using MessageFlow.DataAccess.Services;
using MessageFlow.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.Services;
using MessageFlow.Tests.Helpers.Stubs;

namespace MessageFlow.Tests.Helpers
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
        public static Mock<IHttpContextAccessor> CreateHttpContextAccessor(string userId, string role, string? companyId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            if (!string.IsNullOrEmpty(companyId))
            {
                claims.Add(new Claim("CompanyId", companyId));
            }

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"))
            };

            httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);
            return httpContextAccessorMock;
        }

        //// Create CompanyManagementService
        //public static CompanyManagementService CreateCompanyManagementService(
        //    IUnitOfWork unitOfWork,
        //    IMapper mapper,
        //    string userId,
        //    string role,
        //    IAuthorizationHelper? authHelper = null)
        //{
        //    var loggerMock = new Mock<ILogger<CompanyManagementService>>();
        //    var configMock = new Mock<IConfiguration>();

        //    var companyId = unitOfWork.ApplicationUsers.GetUserCompanyIdAsync(userId).Result;

        //    var httpContextAccessorMock = CreateHttpContextAccessor(userId, role, companyId);

        //    // Use provided helper or create real one
        //    authHelper ??= new AuthorizationHelper(httpContextAccessorMock.Object);

        //    var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        //    var fakeHttpClient = new HttpClient(new HttpMessageHandlerStub())
        //    {
        //        BaseAddress = new Uri("https://fake.identity.api/")
        //    };
        //    httpClientFactoryMock.Setup(f => f.CreateClient("IdentityAPI")).Returns(fakeHttpClient);

        //    var teamsService = new TeamsManagementService(
        //        unitOfWork,
        //        new Mock<ILogger<TeamsManagementService>>().Object,
        //        mapper,
        //        httpClientFactoryMock.Object,
        //        authHelper);

        //    return new CompanyManagementService(
        //        authHelper,
        //        httpClientFactoryMock.Object,
        //        unitOfWork,
        //        loggerMock.Object,
        //        httpContextAccessorMock.Object,
        //        teamsService,
        //        new FakeAzureBlobStorageService(),
        //        new FakeDocumentProcessingService(),
        //        new FakeAzureSearchService(),
        //        mapper
        //    );
        //}

        //// Create TeamsManagementService
        //public static TeamsManagementService CreateTeamsManagementService(
        //    IUnitOfWork unitOfWork,
        //    IMapper mapper,
        //    string userId,
        //    string role)
        //{
        //    var loggerMock = new Mock<ILogger<TeamsManagementService>>();

        //    // ✅ Mock IHttpClientFactory with BaseAddress
        //    var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        //    var fakeHttpClient = new HttpClient(new HttpMessageHandlerStub())
        //    {
        //        BaseAddress = new Uri("https://fake.identity.api/")
        //    };
        //    httpClientFactoryMock.Setup(f => f.CreateClient("IdentityAPI")).Returns(fakeHttpClient);

        //    // ✅ Mock IAuthorizationHelper
        //    var companyId = unitOfWork.ApplicationUsers.GetUserCompanyIdAsync(userId).Result;
        //    var httpContextAccessorMock = CreateHttpContextAccessor(userId, role, companyId);
        //    var authHelper = new AuthorizationHelper(httpContextAccessorMock.Object);

        //    return new TeamsManagementService(
        //        unitOfWork,
        //        loggerMock.Object,
        //        mapper,
        //        httpClientFactoryMock.Object,
        //        authHelper
        //    );
        //}


        //// Create UserManagementService with IUnitOfWork and necessary mocks
        //public static UserManagementService CreateUserManagementService(
        //    IUnitOfWork unitOfWork,
        //    UserManager<ApplicationUser> userManager,
        //    RoleManager<IdentityRole> roleManager,
        //    IMapper mapper,
        //    string userId,
        //    string role)
        //{
        //    var loggerMock = new Mock<ILogger<UserManagementService>>();
        //    var httpContextAccessorMock = CreateHttpContextAccessor(userId, role);

        //    var teamsService = new TeamsManagementService(unitOfWork, new Mock<ILogger<TeamsManagementService>>().Object, mapper);

        //    // ✅ Create UserStore using the context from UnitOfWork
        //    var userStore = new UserStore<ApplicationUser>(unitOfWork.Context);

        //    return new UserManagementService(
        //        unitOfWork,
        //        teamsService,
        //        loggerMock.Object,
        //        httpContextAccessorMock.Object,
        //        mapper
        //    );
        //}

    }
}