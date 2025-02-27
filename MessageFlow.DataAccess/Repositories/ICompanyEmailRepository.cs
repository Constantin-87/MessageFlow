using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ICompanyEmailRepository
    {
        Task<List<CompanyEmail>> GetAllAsync();
        Task<List<CompanyEmail>> GetCompanyEmailsByCompanyIdAsync(string companyId);
        Task<CompanyEmail?> GetByIdStringAsync(string emailId);
        Task AddEntityAsync(CompanyEmail companyEmail);
        Task UpdateEntityAsync(CompanyEmail companyEmail);
        Task UpdateEmailsAsync(string companyId, List<CompanyEmail> companyEmails);
        Task RemoveEntityAsync(CompanyEmail companyEmail);
    }
}
