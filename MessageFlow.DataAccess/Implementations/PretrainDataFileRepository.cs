using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class PretrainDataFileRepository : GenericRepository<PretrainDataFile>, IPretrainDataFileRepository
    {
        private readonly ApplicationDbContext? _context;

        public PretrainDataFileRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

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
