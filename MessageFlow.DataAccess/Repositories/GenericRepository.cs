using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MessageFlow.DataAccess.Repositories
{
    public class GenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext? _context; // For direct context usage
        private readonly IDbContextFactoryService? _dbContextFactory; // For factory usage

        // Constructor for direct context usage
        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Constructor for factory usage
        public GenericRepository(IDbContextFactoryService dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        // Helper: Get context based on availability
        private ApplicationDbContext GetContext()
        {
            if (_context != null) return _context;

            throw new InvalidOperationException("No direct context provided. Use factory-based methods.");
        }

        // ✅ Method using either direct context or factory
        public virtual async Task<List<T>> GetAllAsync()
        {
            if (_context != null)
            {
                // Direct context: single-context scenario (efficient for tracking)
                return await _context.Set<T>().ToListAsync();
            }

            // Factory: fresh context per call (parallel-safe)
            return await _dbContextFactory!.ExecuteScopedAsync(async context =>
                await context.Set<T>().ToListAsync()
            );
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            if (_context != null)
            {
                return await _context.Set<T>().Where(predicate).ToListAsync();
            }

            return await _dbContextFactory!.ExecuteScopedAsync(async context =>
                await context.Set<T>().Where(predicate).ToListAsync()
            );
        }

        public virtual async Task<T?> GetByIdStringAsync(string id)
        {
            if (_context != null)
            {
                return await _context.Set<T>().FindAsync(id);
            }

            return await _dbContextFactory!.ExecuteScopedAsync(async context =>
                await context.Set<T>().FindAsync(id)
            );
        }

        public virtual async Task<List<T>> GetListOfEntitiesByIdStringAsync(IEnumerable<string> ids)
        {
            if (_context != null)
            {
                return await _context.Set<T>()
                    .Where(entity => EF.Property<string>(entity, "Id") != null && ids.Contains(EF.Property<string>(entity, "Id")))
                    .ToListAsync();
            }

            return await _dbContextFactory!.ExecuteScopedAsync(async context =>
                await context.Set<T>()
                    .Where(entity => EF.Property<string>(entity, "Id") != null && ids.Contains(EF.Property<string>(entity, "Id")))
                    .ToListAsync()
            );
        }


        public virtual async Task<T?> GetByIdIntAsync(int id)
        {
            return await _dbContextFactory.ExecuteScopedAsync(async context =>
            {
                return await context.Set<T>().FindAsync(id);
            });
        }

       public virtual async Task AddEntityAsync(T entity)
        {
            if (_context != null)
            {
                await _context.Set<T>().AddAsync(entity); // Tracks changes for batch saving
                return;
            }

            await _dbContextFactory!.ExecuteScopedAsync(async context =>
            {
                await context.Set<T>().AddAsync(entity);
            });
        }

        public virtual async Task UpdateEntityAsync(T entity)
        {
            if (_context != null)
            {
                _context.Set<T>().Update(entity); // Tracks for later SaveChangesAsync()
                return;
            }

            await _dbContextFactory!.ExecuteScopedAsync(context =>
            {
                context.Set<T>().Update(entity);
                return Task.CompletedTask;
            });
        }

        public virtual async Task RemoveEntityAsync(T entity)
        {
            if (_context != null)
            {
                _context.Set<T>().Remove(entity); // Tracks for later SaveChangesAsync()
                return;
            }

            await _dbContextFactory!.ExecuteScopedAsync( context =>
            {
                context.Set<T>().Remove(entity);
                return Task.CompletedTask;
            });
        }
    }
}
