using MessageFlow.DataAccess.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.DataAccess.Services
{
    public class DbContextFactoryService : IDbContextFactoryService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DbContextFactoryService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<T> ExecuteScopedAsync<T>(Func<ApplicationDbContext, Task<T>> action)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await action(context);  // ✅ Fresh DbContext per call
        }

        public async Task ExecuteScopedAsync(Func<ApplicationDbContext, Task> action)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await action(context);  // ✅ Scoped context for void-like methods
        }


    }
}
