using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.AzureServices.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class DeleteCompanyMetadataCommandHandler : IRequestHandler<DeleteCompanyMetadataCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<DeleteCompanyMetadataCommandHandler> _logger;

        public DeleteCompanyMetadataCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IAzureBlobStorageService blobStorageService,
            ILogger<DeleteCompanyMetadataCommandHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(DeleteCompanyMetadataCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, _, _, errorMessage) = await _authorizationHelper.CompanyAccess(request.CompanyId);
                if (!isAuthorized)
                    return (false, errorMessage);

                var files = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(request.CompanyId);
                if (!files.Any())
                    return (false, "No metadata files found for this company.");

                var successfullyDeleted = new List<ProcessedPretrainData>();
                bool allDeleted = true;

                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.FileUrl))
                    {
                        var deleted = await _blobStorageService.DeleteFileAsync(file.FileUrl);
                        if (deleted)
                        {
                            successfullyDeleted.Add(file);
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to delete file from Blob Storage: {file.FileUrl}");
                            allDeleted = false;
                        }
                    }
                }

                if (successfullyDeleted.Any())
                {
                    _unitOfWork.ProcessedPretrainData.RemoveProcessedFiles(successfullyDeleted);
                    await _unitOfWork.SaveChangesAsync();
                }

                return allDeleted
                    ? (true, "All company metadata files deleted successfully.")
                    : (false, "Some files failed to delete from Azure Blob Storage, their database records were retained.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting metadata for company {CompanyId}", request.CompanyId);
                return (false, "An error occurred while deleting metadata.");
            }
        }
    }
}
