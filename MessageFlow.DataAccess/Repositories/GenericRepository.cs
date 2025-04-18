using MessageFlow.DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.DataAccess.Repositories
{
    public class GenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext? _context;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            if (_context != null) return await _context.Set<T>().ToListAsync();

            throw new InvalidOperationException("No context provided.");
        }

        public virtual async Task<T?> GetByIdStringAsync(string id)
        {
            if (_context != null) return await _context.Set<T>().FindAsync(id);

            throw new InvalidOperationException("No context provided.");
        }

        public virtual async Task<List<T>> GetListOfEntitiesByIdStringAsync(IEnumerable<string> ids)
        {
            if (_context != null)
            {
                return await _context.Set<T>()
                    .Where(entity => EF.Property<string>(entity, "Id") != null && ids.Contains(EF.Property<string>(entity, "Id")))
                    .ToListAsync();
            }

            throw new InvalidOperationException("No context provided.");
        }

       public virtual async Task AddEntityAsync(T entity)
        {
            if (_context != null)
            {
                await _context.Set<T>().AddAsync(entity);
                return;
            }
            throw new InvalidOperationException("No context provided.");
        }

        public virtual Task UpdateEntityAsync(T entity)
        {
            if (_context != null)
            {
                _context.Set<T>().Update(entity);
                return Task.CompletedTask;
            }
            throw new InvalidOperationException("No context provided.");
        }

        public virtual Task RemoveEntityAsync(T entity)
        {
            if (_context != null)
            {
                _context.Set<T>().Remove(entity);
                return Task.CompletedTask;
            }
            throw new InvalidOperationException("No context provided.");
        }
    }
}
