using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface ICompanyEmailRepository
    {
        Task UpdateEmailsAsync(string companyId, List<CompanyEmail> companyEmails);
    }
}
