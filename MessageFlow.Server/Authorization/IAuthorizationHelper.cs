
namespace MessageFlow.Server.Authorization
{
    public interface IAuthorizationHelper
    {
        Task<(bool isAuthorized, string? userCompanyId, bool isSuperAdmin, string errorMessage)> CompanyAccess(string companyId);

        Task<(bool isAuthorized, string errorMessage)> TeamAccess(string companyId);

        Task<(bool isAuthorized, string errorMessage)> UserManagementAccess(string targetCompanyId, List<string> requestedRoles);

        // NOT USED !!!!
        string? GetBearerToken();
    }
}
