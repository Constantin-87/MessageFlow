using AutoMapper;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers
{
    public class GetCompanyForUserQueryHandler : IRequestHandler<GetCompanyForUserQuery, CompanyDTO?>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetCompanyForUserQueryHandler> _logger;

        public GetCompanyForUserQueryHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetCompanyForUserQueryHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CompanyDTO?> Handle(GetCompanyForUserQuery request, CancellationToken cancellationToken)
        {
            var (_, companyId, _, _) = await _authorizationHelper.CompanyAccess(string.Empty);

            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning("CompanyId claim not found for the current user.");
                return new CompanyDTO(); // fallback: returns an empty object, not null
            }

            var company = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
            return company != null ? _mapper.Map<CompanyDTO>(company) : null;
        }
    }
}
