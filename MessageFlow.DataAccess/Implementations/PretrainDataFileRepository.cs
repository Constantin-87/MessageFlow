using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class PretrainDataFileRepository : GenericRepository<PretrainDataFile>, IPretrainDataFileRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public PretrainDataFileRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public PretrainDataFileRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<List<PretrainDataFile>> GetAllFilesAsync()
        {
            return await _context.PretrainDataFiles.Include(f => f.Company).ToListAsync();
        }

        public async Task<PretrainDataFile?> GetFileByIdAsync(int fileId)
        {
            return await _context.PretrainDataFiles
                .Include(f => f.Company)
                .FirstOrDefaultAsync(f => f.Id == fileId);
        }
    }
}
