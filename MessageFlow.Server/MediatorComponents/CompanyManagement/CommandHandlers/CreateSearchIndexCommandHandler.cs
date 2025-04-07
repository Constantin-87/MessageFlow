using AutoMapper;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class CreateSearchIndexCommandHandler : IRequestHandler<CreateSearchIndexCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureSearchService _azureSearchService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateSearchIndexCommandHandler> _logger;

        public CreateSearchIndexCommandHandler(
            IUnitOfWork unitOfWork,
            IAzureSearchService azureSearchService,
            IMapper mapper,
            ILogger<CreateSearchIndexCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _azureSearchService = azureSearchService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(CreateSearchIndexCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var processedFiles = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(request.CompanyId);
                if (!processedFiles.Any())
                    return (false, "No processed data found for this company.");

                await _azureSearchService.CreateCompanyIndexAsync(request.CompanyId);

                var processedFilesDTO = _mapper.Map<List<ProcessedPretrainDataDTO>>(processedFiles);
                await _azureSearchService.UploadDocumentsToIndexAsync(request.CompanyId, processedFilesDTO);

                return (true, "Index created and populated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating index for company {CompanyId}", request.CompanyId);
                return (false, "An error occurred while creating the index.");
            }
        }
    }
}
