
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.Authorization
{
    public interface IAuthorizationHelper
    {
        Task<(bool isAuthorized, string? userCompanyId, bool isSuperAdmin, string errorMessage)> CompanyAccess(string companyId);

        Task<(bool isAuthorized, string errorMessage)> CanManageTeam(string companyId);

        string? GetBearerToken();

    }
}
