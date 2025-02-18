using MessageFlow.Server.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateTestDbContext(string databaseName = null)
    {
        databaseName ??= $"SecuredChatDb_Test_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=localhost;Database={databaseName};Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;")
            .Options;

        // Instantiate the DbContext directly
        var context = new ApplicationDbContext(options);

        // Ensure the database is created
        context.Database.EnsureCreated();
        return context;
    }

    public static IDbContextFactory<ApplicationDbContext> CreateTestDbContextFactory(string databaseName = null)
    {
        databaseName ??= $"SecuredChatDb_Test_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=localhost;Database={databaseName};Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;")
            .Options;

        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContext()).Returns(new ApplicationDbContext(options));

        return factoryMock.Object;
    }
}
