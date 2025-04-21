using AutoMapper;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.Authorization;
using MediatR;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;

namespace MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers
{
    public class GetAllCompaniesQueryHandler : IRequestHandler<GetAllCompaniesQuery, List<CompanyDTO>>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllCompaniesQueryHandler> _logger;

        public GetAllCompaniesQueryHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetAllCompaniesQueryHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<CompanyDTO>> Handle(GetAllCompaniesQuery request, CancellationToken cancellationToken)
        {
            var (_, companyId, isSuperAdmin, errorMessage) = await _authorizationHelper.CompanyAccess(string.Empty);

            if (isSuperAdmin)
            {
                var companies = await _unitOfWork.Companies.GetAllCompaniesWithUserCountAsync();
                return _mapper.Map<List<CompanyDTO>>(companies);
            }

            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning("CompanyId claim not found for the current user.");
                return new List<CompanyDTO>();
            }

            var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
            if (company == null)
            {
                errorMessage = $"Company with ID {companyId} not found.";
                _logger.LogError(errorMessage);
                throw new KeyNotFoundException(errorMessage);
            }

            return new List<CompanyDTO> { _mapper.Map<CompanyDTO>(company) };
        }
    }
}
