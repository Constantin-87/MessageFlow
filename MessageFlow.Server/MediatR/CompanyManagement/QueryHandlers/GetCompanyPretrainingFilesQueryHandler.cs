using AutoMapper;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers
{
    public class GetCompanyPretrainingFilesQueryHandler : IRequestHandler<GetCompanyPretrainingFilesQuery, (bool success, List<ProcessedPretrainDataDTO> files, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetCompanyPretrainingFilesQueryHandler> _logger;
        private readonly IAuthorizationHelper _authorizationHelper;

        public GetCompanyPretrainingFilesQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetCompanyPretrainingFilesQueryHandler> logger,
            IAuthorizationHelper authorizationHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _authorizationHelper = authorizationHelper;
        }

        public async Task<(bool success, List<ProcessedPretrainDataDTO> files, string errorMessage)> Handle(GetCompanyPretrainingFilesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, _, _, errorMessage) = await _authorizationHelper.CompanyAccess(request.CompanyId);
                if (!isAuthorized)
                    return (false, new List<ProcessedPretrainDataDTO>(), errorMessage);

                var files = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(request.CompanyId);

                if (!files.Any())
                    return (false, new List<ProcessedPretrainDataDTO>(), "No pretraining files found for this company.");

                var filesDto = _mapper.Map<List<ProcessedPretrainDataDTO>>(files);
                return (true, filesDto, "Files retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pretraining files for company {CompanyId}", request.CompanyId);
                return (false, new List<ProcessedPretrainDataDTO>(), "An error occurred while retrieving files.");
            }
        }
    }
}
