using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MockQueryable.Moq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MockQueryable;

namespace MessageFlow.Tests.Helpers.Factories
{
    public static class UnitTestFactory
    {
        public static ApplicationDbContext CreateInMemoryDbContext(string? dbName = null, bool includeTestEntities = false)
        {
            dbName ??= $"TestDb_{Guid.NewGuid()}";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var context = includeTestEntities
            ? new TestDbContext(options)   // includes TestEntity
            : new ApplicationDbContext(options); // regular app context

            context.Database.EnsureCreated();
            return context;
        }

        public static IUnitOfWork CreateUnitOfWork(ApplicationDbContext? context = null)
        {
            context ??= CreateInMemoryDbContext();
            return new UnitOfWork(context);
        }

        public static Mock<UserManager<ApplicationUser>> CreateMockUserManager(IQueryable<ApplicationUser> users)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var manager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            var dbSet = users.BuildMock().BuildMockDbSet();
            manager.Setup(m => m.Users).Returns(dbSet.Object);
            manager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            manager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((string email) => users.FirstOrDefault(u => u.Email == email));
            manager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "User" });
            manager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            return manager;
        }

        public static UserManager<ApplicationUser> CreateRealUserManager(ApplicationDbContext context)
        {
            var store = new UserStore<ApplicationUser>(context);
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(o => o.Value).Returns(new IdentityOptions());

            return new UserManager<ApplicationUser>(
                store,
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

        public static RoleManager<IdentityRole> CreateRoleManager(ApplicationDbContext context)
        {
            var roleStore = new RoleStore<IdentityRole>(context);
            return new RoleManager<IdentityRole>(
                roleStore,
                Array.Empty<IRoleValidator<IdentityRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object
            );
        }

        public static Mock<IHttpContextAccessor> CreateHttpContextAccessor(string userId, string role, string? companyId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            if (!string.IsNullOrEmpty(companyId))
                claims.Add(new Claim("CompanyId", companyId));

            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(context);

            return accessor;
        }

        public static (IMediator Mediator, Mock<IUnitOfWork> UnitOfWorkMock, Mock<ILogger<ProcessMessageHandler>> LoggerMock) CreateMediatorWithMocks()
        {
            var services = new ServiceCollection();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var loggerMock = new Mock<ILogger<ProcessMessageHandler>>();

            services.AddSingleton(unitOfWorkMock.Object);
            services.AddSingleton(typeof(ILogger<ProcessMessageHandler>), loggerMock.Object);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ProcessMessageHandler>());

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            return (mediator, unitOfWorkMock, loggerMock);
        }

        private class TestDbContext : ApplicationDbContext
        {
            public TestDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

            public DbSet<TestEntity> TestEntities => Set<TestEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
            }
        }
    }

    public class TestEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
    }
}