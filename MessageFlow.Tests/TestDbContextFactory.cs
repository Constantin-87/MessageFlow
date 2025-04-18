using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

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

        var context = new ApplicationDbContext(options);
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
