using System.Data;
using System.Security.Claims;

namespace MessageFlow.Server.Authorization
{
    public class AuthorizationHelper : IAuthorizationHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _superAdminCompanyId;

        public AuthorizationHelper(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _superAdminCompanyId = configuration["SuperAdminCompanyId"] ?? "";
        }

        public Task<(bool isAuthorized, string? userCompanyId, bool isSuperAdmin, string errorMessage)> CompanyAccess(string companyId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return Task.FromResult<(bool, string?, bool, string)>(
                    (false, null, false, "User context not available.")
                );
            }

            var role = user.FindFirstValue(ClaimTypes.Role);
            var companyIdClaim = user.FindFirstValue("CompanyId");

            bool isSuperAdmin = role?.Contains("SuperAdmin") == true;

            // If SuperAdmin, always authorize
            if (isSuperAdmin)
            {
                return Task.FromResult<(bool, string?, bool, string)>(
                    (true, companyId, isSuperAdmin, string.Empty)
                );
            }

            // If no companyId was passed, authorize for the user's companyId found in claims only
            if (string.IsNullOrEmpty(companyId))
            {
                if (string.IsNullOrEmpty(companyIdClaim))
                {
                    return Task.FromResult<(bool, string?, bool, string)>(
                        (false, null, isSuperAdmin, "Company ID missing in user claims.")
                    );
                }

                return Task.FromResult<(bool, string?, bool, string)>(
                        (true, companyIdClaim, isSuperAdmin, string.Empty)
                    );
            }

            if (companyIdClaim == null || companyIdClaim != companyId)
            {
                return Task.FromResult<(bool, string?, bool, string)>(
                    (false, companyIdClaim, false, "Unauthorized for this company.")
                );
            }

            return Task.FromResult<(bool, string?, bool, string)>(
                (true, companyIdClaim, false, string.Empty)
            );
        }

        public Task<(bool isAuthorized, string errorMessage)> TeamAccess(string companyId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return Task.FromResult((false, "User context not available."));
            }

            var role = user.FindFirstValue(ClaimTypes.Role);
            var userCompanyId = user.FindFirstValue("CompanyId");
            bool isSuperAdmin = role?.Contains("SuperAdmin") == true;

            if (isSuperAdmin)
            {
                return Task.FromResult((true, string.Empty));
            }

            if ( userCompanyId == companyId)
            {
                return Task.FromResult((true, string.Empty));
            }

            return Task.FromResult((false, "Unauthorized: Cannot manage Teams for other Companies."));
        }

        public Task<(bool isAuthorized, string errorMessage)> UserManagementAccess(string targetCompanyId, List<string> requestedRoles)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return Task.FromResult((false, "User context not available."));

            var role = user.FindFirstValue(ClaimTypes.Role);
            var currentCompanyId = user.FindFirstValue("CompanyId");

            bool isSuperAdmin = role?.Contains("SuperAdmin") == true;
            bool isAdmin = role?.Contains("Admin") == true;
            bool isTryingToAssignSuperAdmin = requestedRoles.Any(r => r == "SuperAdmin");

            // SuperAdmin can manage any user, but SuperAdmin role can only be assigned to users in company MessageFlow
            if (isSuperAdmin)
            {
                if (isTryingToAssignSuperAdmin && targetCompanyId != _superAdminCompanyId)
                    return Task.FromResult((false, "SuperAdmin role can only be assigned to the MessageFlow users."));

                return Task.FromResult((true, string.Empty));
            }

            // Admins can only manage users in their company
            if (isAdmin && currentCompanyId == targetCompanyId)
            {
                if (isTryingToAssignSuperAdmin)
                    return Task.FromResult((false, "Admins cannot assign the SuperAdmin role."));

                return Task.FromResult((true, string.Empty));
            }

            return Task.FromResult((false, "Unauthorized to manage users for this company."));
        }

        public Task<(bool isAuthorized, string errorMessage)> ChannelSettingsAccess(string companyId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return Task.FromResult((false, "User context not available."));

            var userCompanyId = user.FindFirstValue("CompanyId");
            var role = user.FindFirstValue(ClaimTypes.Role);

            if (role?.Contains("SuperAdmin") == true)
                return Task.FromResult((true, string.Empty));

            if (userCompanyId == companyId)
                return Task.FromResult((true, string.Empty));

            return Task.FromResult((false, "Unauthorized to manage channel settings for this company."));
        }
    }
}