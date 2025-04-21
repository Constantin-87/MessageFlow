using MessageFlow.DataAccess.Repositories;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;

public class GenericRepositoryTests
{
    private TestDbContext CreateContext() => (TestDbContext)TestDbContextFactory.CreateDbContext();

    [Fact]
    public async Task AddEntityAsync_AddsEntity()
    {
        using var context = CreateContext();
        var repo = new GenericRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Sample" };

        await repo.AddEntityAsync(entity);
        await context.SaveChangesAsync();

        Assert.Single(context.TestEntities);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        using var context = CreateContext();
        context.TestEntities.AddRange(new TestEntity(), new TestEntity());
        await context.SaveChangesAsync();

        var repo = new GenericRepository<TestEntity>(context);
        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdStringAsync_ReturnsCorrectEntity()
    {
        using var context = CreateContext();
        var entity = new TestEntity();
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<TestEntity>(context);
        var result = await repo.GetByIdStringAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
    }

    [Fact]
    public async Task GetListOfEntitiesByIdStringAsync_ReturnsMatchingEntities()
    {
        using var context = CreateContext();
        var e1 = new TestEntity();
        var e2 = new TestEntity();
        var e3 = new TestEntity();
        context.TestEntities.AddRange(e1, e2, e3);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<TestEntity>(context);
        var result = await repo.GetListOfEntitiesByIdStringAsync(new[] { e1.Id, e3.Id });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == e1.Id);
        Assert.Contains(result, x => x.Id == e3.Id);
    }

    [Fact]
    public async Task UpdateEntityAsync_UpdatesEntity()
    {
        using var context = CreateContext();
        var entity = new TestEntity { Name = "Old" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<TestEntity>(context);
        entity.Name = "New";
        await repo.UpdateEntityAsync(entity);
        await context.SaveChangesAsync();

        var updated = await context.TestEntities.FindAsync(entity.Id);
        Assert.Equal("New", updated!.Name);
    }

    [Fact]
    public async Task RemoveEntityAsync_RemovesEntity()
    {
        using var context = CreateContext();
        var entity = new TestEntity();
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<TestEntity>(context);
        await repo.RemoveEntityAsync(entity);
        await context.SaveChangesAsync();

        Assert.Empty(context.TestEntities);
    }
}