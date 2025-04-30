using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Security.Claims;

public class TestDbContext : ApplicationDbContext
{
    public TestDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Add DbSet for test entities here
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
    }
}

public static class TestDbContextFactory
{
    public static IUnitOfWork CreateUnitOfWork(
        ApplicationDbContext context = null,
        UserManager<ApplicationUser> userManager = null,
        IUserStore<ApplicationUser> userStore = null
    )
    {
        context ??= CreateDbContext();

        // Mock UserManager if not provided
        userStore ??= new Mock<IUserStore<ApplicationUser>>().Object;
        userManager ??= userManager ??= CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable()).Object;

        return new UnitOfWork(context);
    }

    public static ApplicationDbContext CreateDbContext(string databaseName = null)
    {
        databaseName ??= $"TestDb_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }


    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager(IQueryable<ApplicationUser> users)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null
        );

        var dbSet = users.BuildMock().BuildMockDbSet();
        userManagerMock.Setup(u => u.Users).Returns(dbSet.Object);

        userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => users.FirstOrDefault(u => u.Email == email));

        userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(u => u.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(u => u.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(u => u.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        return userManagerMock;
    }

    //private static UserManager<ApplicationUser> MockUserManager(IUserStore<ApplicationUser> userStore)
    //{
    //    var userManagerMock = new Mock<UserManager<ApplicationUser>>(
    //        userStore,
    //        null, null, null, null, null, null, null, null
    //    );

    //    userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
    //        .ReturnsAsync(IdentityResult.Success);

    //    userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
    //        .ReturnsAsync((string userId) => new ApplicationUser { Id = userId });

    //    userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
    //        .ReturnsAsync(new List<string> { "User" });

    //    return userManagerMock.Object;
    //}

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

    // Create a mocked SignInManager using IUnitOfWork
    //public static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
    //{
    //    var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    //    var httpContext = new DefaultHttpContext();

    //    var serviceProviderMock = new Mock<IServiceProvider>();
    //    var authenticationServiceMock = new Mock<IAuthenticationService>();

    //    serviceProviderMock
    //        .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
    //        .Returns(authenticationServiceMock.Object);

    //    httpContext.RequestServices = serviceProviderMock.Object;
    //    httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);

    //    return new SignInManager<ApplicationUser>(
    //        userManager,
    //        httpContextAccessorMock.Object,
    //        new UserClaimsPrincipalFactory<ApplicationUser>(userManager, new Mock<IOptions<IdentityOptions>>().Object),
    //        null,
    //        new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
    //        null,
    //        null
    //    );
    //}

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

    public static UserManager<ApplicationUser> CreateRealUserManager(ApplicationDbContext context)
    {
        var userStore = new UserStore<ApplicationUser>(context);
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new UserManager<ApplicationUser>(
            userStore,
            options.Object,
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object
        );
    }
}

public class TestEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}