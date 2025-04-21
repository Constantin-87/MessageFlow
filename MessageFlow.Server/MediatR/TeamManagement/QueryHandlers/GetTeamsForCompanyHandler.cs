using AutoMapper;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.TeamManagement.Queries;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.TeamManagement.QueryHandlers
{
    public class GetTeamsForCompanyHandler : IRequestHandler<GetTeamsForCompanyQuery, List<TeamDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTeamsForCompanyHandler> _logger;

        public GetTeamsForCompanyHandler(IUnitOfWork unitOfWork, IAuthorizationHelper auth, IMapper mapper, ILogger<GetTeamsForCompanyHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<TeamDTO>> Handle(GetTeamsForCompanyQuery request, CancellationToken cancellationToken)
        {
            var (isAuthorized, errorMessage) = await _auth.TeamAccess(request.CompanyId);
            if (!isAuthorized)
            {
                _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
                return new List<TeamDTO>();
            }

            var teams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(request.CompanyId);
            return _mapper.Map<List<TeamDTO>>(teams);
        }
    }
}
