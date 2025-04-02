using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using System.Security.Claims;

namespace MessageFlow.Server.Authorization
{
    public class AuthorizationHelper : IAuthorizationHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizationHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
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

            //var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
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

            // Compare accessed companyId with user's companyID from claim
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

        public Task<(bool isAuthorized, string errorMessage)> CanManageTeam(string companyId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return Task.FromResult((false, "User context not available."));
            }

            var userCompanyId = user.FindFirstValue("CompanyId");

            if ( userCompanyId == companyId)
            {
                return Task.FromResult((true, string.Empty));
            }

            return Task.FromResult((false, "Unauthorized: Cannot manage Teams for other Companies."));
        }

        public string? GetBearerToken()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length);
            }

            return null;
        }

    }
}
