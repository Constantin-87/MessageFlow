using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Queries;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.QueryHandlers
{
    public class GetCompanyMetadataQueryHandler : IRequestHandler<GetCompanyMetadataQuery, (bool success, string metadata, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<GetCompanyMetadataQueryHandler> _logger;

        public GetCompanyMetadataQueryHandler(
            IAuthorizationHelper authorizationHelper,
            IAzureBlobStorageService blobStorageService,
            ILogger<GetCompanyMetadataQueryHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<(bool success, string metadata, string errorMessage)> Handle(GetCompanyMetadataQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, _, _, errorMessage) = await _authorizationHelper.CompanyAccess(request.CompanyId);
                if (!isAuthorized)
                    return (false, string.Empty, errorMessage);

                var metadataContent = await _blobStorageService.GetAllCompanyRagDataFilesAsync(request.CompanyId);
                if (string.IsNullOrEmpty(metadataContent))
                    return (false, string.Empty, "Metadata not found.");

                return (true, metadataContent, "Metadata retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metadata for company {CompanyId}", request.CompanyId);
                return (false, string.Empty, "An error occurred while retrieving metadata.");
            }
        }
    }
}
