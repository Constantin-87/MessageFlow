using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Repositories;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories
{
    public class GenericRepositoryTests
    {
        private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext(includeTestEntities: true);

        // Test for AddEntityAsync
        [Fact]
        public async Task AddEntityAsync_AddsEntity()
        {
            using var context = CreateContext();
            var repo = new GenericRepository<TestEntity>(context);
            var entity = new TestEntity { Name = "Sample" };

            await repo.AddEntityAsync(entity);
            await context.SaveChangesAsync();

            Assert.Single(context.Set<TestEntity>());
        }

        // Test for GetAllAsync
        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            using var context = CreateContext();
            context.Set<TestEntity>().AddRange(new TestEntity(), new TestEntity());
            await context.SaveChangesAsync();

            var repo = new GenericRepository<TestEntity>(context);
            var result = await repo.GetAllAsync();

            Assert.Equal(2, result.Count);
        }

        // Test for GetByIdStringAsync
        [Fact]
        public async Task GetByIdStringAsync_ReturnsCorrectEntity()
        {
            using var context = CreateContext();
            var entity = new TestEntity();
            context.Set<TestEntity>().Add(entity);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<TestEntity>(context);
            var result = await repo.GetByIdStringAsync(entity.Id);

            Assert.NotNull(result);
            Assert.Equal(entity.Id, result!.Id);
        }

        // Test for GetListOfEntitiesByIdStringAsync
        [Fact]
        public async Task GetListOfEntitiesByIdStringAsync_ReturnsMatchingEntities()
        {
            using var context = CreateContext();
            var e1 = new TestEntity();
            var e2 = new TestEntity();
            var e3 = new TestEntity();
            context.Set<TestEntity>().AddRange(e1, e2, e3);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<TestEntity>(context);
            var result = await repo.GetListOfEntitiesByIdStringAsync(new[] { e1.Id, e3.Id });

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == e1.Id);
            Assert.Contains(result, x => x.Id == e3.Id);
        }

        // Test for UpdateEntityAsync
        [Fact]
        public async Task UpdateEntityAsync_UpdatesEntity()
        {
            using var context = CreateContext();
            var entity = new TestEntity { Name = "Old" };
            context.Set<TestEntity>().Add(entity);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<TestEntity>(context);
            entity.Name = "New";
            await repo.UpdateEntityAsync(entity);
            await context.SaveChangesAsync();

            var updated = await context.Set<TestEntity>().FindAsync(entity.Id);
            Assert.Equal("New", updated!.Name);
        }

        // Test for RemoveEntityAsync
        [Fact]
        public async Task RemoveEntityAsync_RemovesEntity()
        {
            using var context = CreateContext();
            var entity = new TestEntity();
            context.Set<TestEntity>().Add(entity);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<TestEntity>(context);
            await repo.RemoveEntityAsync(entity);
            await context.SaveChangesAsync();

            Assert.Empty(context.Set<TestEntity>());
        }

        [Fact]
        public void Constructor_ThrowsException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new GenericRepository<TestEntity>(null!));
            Assert.Equal("Value cannot be null. (Parameter 'context')", exception.Message);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenContextIsNull()
        {
            // Assert that the ArgumentNullException is thrown when the constructor is called with a null context
            var exception = Assert.Throws<ArgumentNullException>(() => new GenericRepository<TestEntity>(null!));
            Assert.Equal("Value cannot be null. (Parameter 'context')", exception.Message);
        }
    }
}