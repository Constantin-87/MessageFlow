using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers
{
    public class AddTeamToCompanyHandler : IRequestHandler<AddTeamToCompanyCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<AddTeamToCompanyHandler> _logger;

        public AddTeamToCompanyHandler(
            IUnitOfWork unitOfWork,
            IAuthorizationHelper auth,
            ILogger<AddTeamToCompanyHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(AddTeamToCompanyCommand request, CancellationToken cancellationToken)
        {
            var teamDto = request.TeamDto;

            var (isAuthorized, errorMessage) = await _auth.TeamAccess(teamDto.CompanyId);
            if (!isAuthorized)
            {
                _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
                return (false, errorMessage);
            }

            List<ApplicationUser> trackedUsers = new();
            if (teamDto.AssignedUsersDTO is { Count: > 0 })
            {
                var userIds = teamDto.AssignedUsersDTO.Select(u => u.Id).ToList();

                trackedUsers = await _unitOfWork.ApplicationUsers.GetListOfEntitiesByIdStringAsync(userIds);

                if (trackedUsers == null || trackedUsers.Count == 0)
                {
                    _logger.LogError("Identity Service returned null users list.");
                    return (false, "An error occurred while retrieving the users.");
                }

            }

            var team = new Team
            {
                Id = Guid.NewGuid().ToString(),
                TeamName = teamDto.TeamName,
                TeamDescription = teamDto.TeamDescription,
                CompanyId = teamDto.CompanyId,
                Users = trackedUsers
            };

            await _unitOfWork.Teams.AddEntityAsync(team);
            await _unitOfWork.SaveChangesAsync();

            return (true, "Team added successfully.");
        }
    }
}
