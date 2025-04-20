using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.Shared.DTOs;
using MessageFlow.Shared.Enums;
using System.Text;
using MessageFlow.AzureServices.Helpers;
using MediatR;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class GenerateCompanyMetadataCommandHandler : IRequestHandler<GenerateCompanyMetadataCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<GenerateCompanyMetadataCommandHandler> _logger;

        public GenerateCompanyMetadataCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAzureBlobStorageService blobStorageService,
            ILogger<GenerateCompanyMetadataCommandHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(GenerateCompanyMetadataCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, _, _, errorMessage) = await _authorizationHelper.CompanyAccess(request.CompanyId);
                if (!isAuthorized)
                    return (false, errorMessage);

                var company = await _unitOfWork.Companies.GetByIdStringAsync(request.CompanyId);
                if (company == null)
                    return (false, "Company not found.");

                var companyDto = _mapper.Map<CompanyDTO>(company);

                var existingFiles = await _unitOfWork.ProcessedPretrainData
                    .GetProcessedFilesByCompanyIdAndTypesAsync(request.CompanyId, new List<FileType>
                    {
                        FileType.CompanyEmails,
                        FileType.CompanyDetails,
                        FileType.CompanyPhoneNumbers
                    });

                foreach (var file in existingFiles)
                {
                    if (!string.IsNullOrEmpty(file.FileUrl))
                        await _blobStorageService.DeleteFileAsync(file.FileUrl);
                }

                _unitOfWork.ProcessedPretrainData.RemoveProcessedFiles(existingFiles);
                await _unitOfWork.SaveChangesAsync();

                var (processedFilesDTO, jsonContents) = CompanyDataHelper.GenerateStructuredCompanyMetadata(companyDto);
                if (processedFilesDTO.Count != jsonContents.Count)
                    return (false, "Mismatch between processed metadata files and JSON contents.");

                string baseFolderPath = "CompanyRAGData/";

                for (int i = 0; i < processedFilesDTO.Count; i++)
                {
                    var processedFile = processedFilesDTO[i];
                    var jsonContent = jsonContents[i];

                    using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                    string jsonFileName = $"{baseFolderPath}metadata_{processedFile.Id}.json";
                    string jsonFileUrl = await _blobStorageService.UploadFileAsync(jsonStream, jsonFileName, "application/json", request.CompanyId);

                    processedFile.FileUrl = jsonFileUrl;
                }

                var processedFiles = _mapper.Map<List<ProcessedPretrainData>>(processedFilesDTO);
                await _unitOfWork.ProcessedPretrainData.AddProcessedFilesAsync(processedFiles);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Company metadata structured and uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and uploading metadata for company {CompanyId}", request.CompanyId);
                return (false, "An error occurred while generating metadata.");
            }
        }
    }
}
