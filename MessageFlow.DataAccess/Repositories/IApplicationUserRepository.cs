using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IApplicationUserRepository
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<List<ApplicationUser>> GetListOfEntitiesByIdStringAsync(IEnumerable<string> ids);
    }
}