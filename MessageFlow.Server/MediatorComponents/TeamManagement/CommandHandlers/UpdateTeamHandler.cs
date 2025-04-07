using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers
{
    public class UpdateTeamHandler : IRequestHandler<UpdateTeamCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<UpdateTeamHandler> _logger;

        public UpdateTeamHandler(IUnitOfWork unitOfWork, IAuthorizationHelper auth, ILogger<UpdateTeamHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = request.TeamDto;
                var (isAuthorized, errorMessage) = await _auth.CanManageTeam(dto.CompanyId);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized team update attempt: " + errorMessage);
                    return (false, errorMessage);
                }

                var team = await _unitOfWork.Teams.GetTeamByIdAsync(dto.Id);
                if (team == null)
                    return (false, "Team not found.");

                team.TeamName = dto.TeamName;
                team.TeamDescription = dto.TeamDescription;
                team.Users.Clear();

                if (dto.AssignedUserIds?.Any() == true)
                {
                    var users = await _unitOfWork.ApplicationUsers.GetListOfEntitiesByIdStringAsync(dto.AssignedUserIds);
                    if (users == null || !users.Any())
                    {
                        _logger.LogError("No valid users found for the provided IDs.");
                        return (false, "No valid users found.");
                    }

                    foreach (var user in users)
                        team.Users.Add(user);
                }

                await _unitOfWork.Teams.UpdateEntityAsync(team);
                await _unitOfWork.SaveChangesAsync();
                return (true, "Team updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team");
                return (false, "An error occurred while updating the team.");
            }
        }
    }
}
