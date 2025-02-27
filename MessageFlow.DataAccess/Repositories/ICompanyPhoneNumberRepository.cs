using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ICompanyPhoneNumberRepository
    {
        Task<List<CompanyPhoneNumber>> GetAllAsync();
        Task<CompanyPhoneNumber?> GetByIdStringAsync(string phoneNumberId);
        Task AddEntityAsync(CompanyPhoneNumber companyPhoneNumber);
        Task UpdateEntityAsync(CompanyPhoneNumber companyPhoneNumber);
        Task RemoveEntityAsync(CompanyPhoneNumber companyPhoneNumber);
        Task UpdatePhoneNumbersAsync(string companyId, List<CompanyPhoneNumber> companyPhoneNumbers); // ✅ New Method
    }
}
