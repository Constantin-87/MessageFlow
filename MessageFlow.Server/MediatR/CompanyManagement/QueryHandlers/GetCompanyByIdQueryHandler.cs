using AutoMapper;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.Authorization;
using MediatR;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;

namespace MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers
{
    public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDTO?>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetCompanyByIdQueryHandler> _logger;

        public GetCompanyByIdQueryHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetCompanyByIdQueryHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CompanyDTO?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
        {
            var (isAuthorized, _, isSuperAdmin, _) = await _authorizationHelper.CompanyAccess(request.CompanyId);

            if (!isAuthorized && !isSuperAdmin)
            {
                _logger.LogWarning("Unauthorized access attempt to company {CompanyId}", request.CompanyId);
                return null;
            }

            var company = await _unitOfWork.Companies.GetCompanyWithDetailsByIdAsync(request.CompanyId);
            return company != null ? _mapper.Map<CompanyDTO>(company) : null;
        }
    }
}
