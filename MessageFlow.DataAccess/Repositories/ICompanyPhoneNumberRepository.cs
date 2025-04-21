using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ICompanyPhoneNumberRepository
    {
        Task UpdatePhoneNumbersAsync(string companyId, List<CompanyPhoneNumber> companyPhoneNumbers);
    }
}