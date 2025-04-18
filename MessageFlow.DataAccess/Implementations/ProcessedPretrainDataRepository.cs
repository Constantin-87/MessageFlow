using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.Shared.Enums;

namespace MessageFlow.DataAccess.Implementations
{
    public class ProcessedPretrainDataRepository : GenericRepository<ProcessedPretrainData>, IProcessedPretrainDataRepository
    {
        private readonly ApplicationDbContext? _context;

        public ProcessedPretrainDataRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<ProcessedPretrainData>> GetProcessedFilesByCompanyIdAndTypesAsync(string companyId, List<FileType> fileTypes)
        {
            return await _context.ProcessedPretrainData
                .Where(f => f.CompanyId == companyId && fileTypes.Contains(f.FileType))
                .ToListAsync();
        }

        public async Task<List<ProcessedPretrainData>> GetProcessedFilesByCompanyIdAsync(string companyId)
        {
            return await _context.ProcessedPretrainData
                .Where(f => f.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task AddProcessedFilesAsync(List<ProcessedPretrainData> processedFiles)
        {
            await _context.ProcessedPretrainData.AddRangeAsync(processedFiles);
        }

        public void RemoveProcessedFiles(List<ProcessedPretrainData> processedFiles)
        {
            _context.ProcessedPretrainData.RemoveRange(processedFiles);
        }
    }
}
