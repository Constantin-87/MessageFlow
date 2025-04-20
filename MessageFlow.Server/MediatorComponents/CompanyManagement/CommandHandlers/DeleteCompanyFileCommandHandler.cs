using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.AzureServices.Interfaces;
using MediatR;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class DeleteCompanyFileCommandHandler : IRequestHandler<DeleteCompanyFileCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<DeleteCompanyFileCommandHandler> _logger;
        private readonly IAuthorizationHelper _authorizationHelper;

        public DeleteCompanyFileCommandHandler(
            IUnitOfWork unitOfWork,
            IAzureBlobStorageService blobStorageService,
            ILogger<DeleteCompanyFileCommandHandler> logger,
            IAuthorizationHelper authorizationHelper)
        {
            _unitOfWork = unitOfWork;
            _blobStorageService = blobStorageService;
            _logger = logger;
            _authorizationHelper = authorizationHelper;
        }

        public async Task<bool> Handle(DeleteCompanyFileCommand request, CancellationToken cancellationToken)
        {
            var file = request.File;
            var companyId = file.CompanyId;
            try
            {
                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) =
                    await _authorizationHelper.CompanyAccess(companyId);

                if (!isAuthorized || (!isSuperAdmin && userCompanyId != companyId))
                {
                    _logger.LogWarning("Unauthorized file delete attempt by user on company {CompanyId}", companyId);
                    return false;
                }
                var fileRecord = await _unitOfWork.ProcessedPretrainData.GetByIdStringAsync(file.Id);
                if (fileRecord == null) return false;

                var deleted = await _blobStorageService.DeleteFileAsync(file.FileUrl);
                if (deleted)
                {
                    await _unitOfWork.ProcessedPretrainData.RemoveEntityAsync(fileRecord);
                    await _unitOfWork.SaveChangesAsync();
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file for company {CompanyId}", request.File.CompanyId);
                return false;
            }
        }
    }
}
