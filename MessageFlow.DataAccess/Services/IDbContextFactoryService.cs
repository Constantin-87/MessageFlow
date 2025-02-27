using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Services
{
    public interface IDbContextFactoryService
    {
        Task<T> ExecuteScopedAsync<T>(Func<ApplicationDbContext, Task<T>> action);
        Task ExecuteScopedAsync(Func<ApplicationDbContext, Task> action);
    }

}
