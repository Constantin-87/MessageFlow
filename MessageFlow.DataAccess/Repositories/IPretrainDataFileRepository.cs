using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IPretrainDataFileRepository
    {
        Task<List<PretrainDataFile>> GetAllFilesAsync();
        Task<PretrainDataFile?> GetFileByIdAsync(int fileId);
        Task AddEntityAsync(PretrainDataFile file);
        Task UpdateEntityAsync(PretrainDataFile file);
        Task RemoveEntityAsync(PretrainDataFile file);
    }
}
