using AutoMapper;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.TeamManagement.Queries;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.TeamManagement.QueryHandlers
{
    public class GetUsersForTeamHandler : IRequestHandler<GetUsersForTeamQuery, List<ApplicationUserDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUsersForTeamHandler> _logger;

        public GetUsersForTeamHandler(IUnitOfWork unitOfWork, IAuthorizationHelper auth, IMapper mapper, ILogger<GetUsersForTeamHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<ApplicationUserDTO>> Handle(GetUsersForTeamQuery request, CancellationToken cancellationToken)
        {
            var team = await _unitOfWork.Teams.GetTeamByIdAsync(request.TeamId);
            if (team == null)
            {
                _logger.LogWarning($"Team with ID {request.TeamId} not found.");
                return new();
            }

            var (isAuthorized, errorMessage) = await _auth.TeamAccess(team.CompanyId);
            if (!isAuthorized)
            {
                _logger.LogWarning($"Unauthorized access to team users: {errorMessage}");
                return new();
            }

            var users = await _unitOfWork.Teams.GetUsersByTeamIdAsync(request.TeamId);
            return _mapper.Map<List<ApplicationUserDTO>>(users);
        }
    }
}