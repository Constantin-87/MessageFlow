using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers
{
    public class AddTeamToCompanyHandler : IRequestHandler<AddTeamToCompanyCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<AddTeamToCompanyHandler> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;

        public AddTeamToCompanyHandler(
            IUnitOfWork unitOfWork,
            IAuthorizationHelper auth,
            ILogger<AddTeamToCompanyHandler> logger,
            IHttpClientFactory httpClientFactory,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
        }

        public async Task<(bool success, string errorMessage)> Handle(AddTeamToCompanyCommand request, CancellationToken cancellationToken)
        {
            var teamDto = request.TeamDto;

            var (isAuthorized, errorMessage) = await _auth.CanManageTeam(teamDto.CompanyId);
            if (!isAuthorized)
            {
                _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
                return (false, errorMessage);
            }

            List<ApplicationUser> mappedUsers = new();
            if (teamDto.AssignedUserIds != null && teamDto.AssignedUserIds.Any())
            {
                var client = _httpClientFactory.CreateClient("IdentityAPI");
                var response = await client.PostAsJsonAsync("api/user-management/get-users-by-ids", teamDto.AssignedUserIds);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch users from Identity Service.");
                    return (false, "An error occurred while retrieving the users.");
                }

                var existingUsers = await response.Content.ReadFromJsonAsync<List<ApplicationUserDTO>>();
                if (existingUsers == null)
                {
                    _logger.LogError("Identity Service returned null users list.");
                    return (false, "An error occurred while retrieving the users.");
                }

                mappedUsers = _mapper.Map<List<ApplicationUser>>(existingUsers);
            }

            var team = new Team
            {
                Id = Guid.NewGuid().ToString(),
                TeamName = teamDto.TeamName,
                TeamDescription = teamDto.TeamDescription,
                CompanyId = teamDto.CompanyId,
                Users = mappedUsers
            };

            await _unitOfWork.Teams.AddEntityAsync(team);
            await _unitOfWork.SaveChangesAsync();

            //_logger.LogInformation($"Team '{team.TeamName}' added successfully to company {team.CompanyId}.");
            return (true, "Team added successfully.");
        }
    }
}
