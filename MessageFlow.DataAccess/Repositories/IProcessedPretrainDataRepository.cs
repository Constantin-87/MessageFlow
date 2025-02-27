using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.Enums;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IProcessedPretrainDataRepository
    {
        Task<List<ProcessedPretrainData>> GetAllAsync();
        Task<List<ProcessedPretrainData>> GetProcessedFilesByCompanyIdAsync(string companyId);
        Task<List<ProcessedPretrainData>> GetProcessedFilesByCompanyIdAndTypesAsync(string companyId, List<FileType> fileTypes);
        Task AddEntityAsync(ProcessedPretrainData data);
        Task UpdateEntityAsync(ProcessedPretrainData data);
        Task RemoveEntityAsync(ProcessedPretrainData data);
        Task AddProcessedFilesAsync(List<ProcessedPretrainData> processedFiles);
        Task <ProcessedPretrainData> GetByIdStringAsync(string fileId);
        void RemoveProcessedFiles(List<ProcessedPretrainData> processedFiles);
    }
}
