using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

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
        userManager ??= MockUserManager(userStore);

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

    private static UserManager<ApplicationUser> MockUserManager(IUserStore<ApplicationUser> userStore)
    {
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore,
            null, null, null, null, null, null, null, null
        );

        userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) => new ApplicationUser { Id = userId });

        userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        return userManagerMock.Object;
    }
}

public class TestEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}