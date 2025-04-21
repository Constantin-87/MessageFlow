using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.AzureServices.Interfaces;
using System.Text;
using MediatR;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.AzureServices.Helpers.Interfaces;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers
{
    public class UploadCompanyFilesCommandHandler : IRequestHandler<UploadCompanyFilesCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<UploadCompanyFilesCommandHandler> _logger;
        private readonly ICompanyDataHelper _companyDataHelper;
        private readonly IAuthorizationHelper _authorizationHelper;

        public UploadCompanyFilesCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDocumentProcessingService documentProcessingService,
            IAzureBlobStorageService blobStorageService,
            ILogger<UploadCompanyFilesCommandHandler> logger,
            ICompanyDataHelper companyDataHelper,
            IAuthorizationHelper authorizationHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentProcessingService = documentProcessingService;
            _blobStorageService = blobStorageService;
            _logger = logger;
            _companyDataHelper = companyDataHelper;
            _authorizationHelper = authorizationHelper;
        }

        public async Task<(bool success, string errorMessage)> Handle(UploadCompanyFilesCommand request, CancellationToken cancellationToken)
        {
            var firstFile = request.Files.FirstOrDefault();
            var companyId = firstFile?.CompanyId ?? string.Empty;

            try
            {
                var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (company == null)
                    return (false, "Company not found.");

                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await _authorizationHelper.CompanyAccess(companyId);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized attempt to upload files for company {CompanyId}. Reason: {Error}", companyId, errorMessage);
                    return (false, errorMessage);
                }

                var (processedFilesDTO, jsonContents) = await _companyDataHelper.ProcessUploadedFilesAsync(request.Files, _documentProcessingService);
                var processedFiles = _mapper.Map<List<ProcessedPretrainData>>(processedFilesDTO);

                if (processedFiles.Count != jsonContents.Count)
                    return (false, "Mismatch between processed files and JSON contents.");

                string baseFolderPath = "CompanyRAGData/";

                for (int i = 0; i < processedFiles.Count; i++)
                {
                    var processedFile = processedFiles[i];
                    var jsonContent = jsonContents[i];

                    using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                    string jsonFileName = $"{baseFolderPath}company_{companyId}_pretrain_{processedFile.Id}.json";
                    string jsonFileUrl = await _blobStorageService.UploadFileAsync(jsonStream, jsonFileName, "application/json", companyId);

                    processedFile.FileUrl = jsonFileUrl;
                }

                await _unitOfWork.ProcessedPretrainData.AddProcessedFilesAsync(processedFiles);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading pretraining files for company {CompanyId}", companyId);
                return (false, "An error occurred during file upload.");
            }
        }
    }
}
