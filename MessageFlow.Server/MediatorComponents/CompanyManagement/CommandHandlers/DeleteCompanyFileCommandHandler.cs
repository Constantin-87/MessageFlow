using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class DeleteCompanyFileCommandHandler : IRequestHandler<DeleteCompanyFileCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<DeleteCompanyFileCommandHandler> _logger;

        public DeleteCompanyFileCommandHandler(
            IUnitOfWork unitOfWork,
            IAzureBlobStorageService blobStorageService,
            ILogger<DeleteCompanyFileCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCompanyFileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var file = request.File;
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
