using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ICompanyRepository
    {
        Task<List<Company>> GetAllAsync();
        Task<List<Company>> GetAllCompaniesWithUserCountAsync();
        Task<Company?> GetByIdStringAsync(string companyId);
        Task<Company?> GetCompanyWithDetailsByIdAsync(string companyId);
        Task AddEntityAsync(Company company);
        Task UpdateEntityAsync(Company company);
        Task RemoveEntityAsync(Company company);
    }
}
