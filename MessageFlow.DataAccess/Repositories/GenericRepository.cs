using MessageFlow.DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.DataAccess.Repositories
{
    public class GenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext? _context;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual async Task<List<T>> GetAllAsync() =>
        await _context.Set<T>().ToListAsync();

        public virtual async Task<T?> GetByIdStringAsync(string id) =>
            await _context.Set<T>().FindAsync(id);

        public virtual async Task<List<T>> GetListOfEntitiesByIdStringAsync(IEnumerable<string> ids) =>
            await _context.Set<T>()
                .Where(entity => EF.Property<string>(entity, "Id") != null && ids.Contains(EF.Property<string>(entity, "Id")))
                .ToListAsync();

        public virtual async Task AddEntityAsync(T entity) =>
            await _context.Set<T>().AddAsync(entity);

        public virtual Task UpdateEntityAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task RemoveEntityAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }
    }
}