using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers
{
    public class DeleteTeamByIdHandler : IRequestHandler<DeleteTeamByIdCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<DeleteTeamByIdHandler> _logger;

        public DeleteTeamByIdHandler(IUnitOfWork unitOfWork, IAuthorizationHelper auth, ILogger<DeleteTeamByIdHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(DeleteTeamByIdCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var team = await _unitOfWork.Teams.GetTeamByIdAsync(request.TeamId);
                if (team == null)
                    return (false, "Team not found.");

                var (isAuthorized, errorMessage) = await _auth.TeamAccess(team.CompanyId);
                if (!isAuthorized)
                {
                    _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
                    return (false, errorMessage);
                }

                _unitOfWork.Teams.RemoveEntityAsync(team);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Team with ID {request.TeamId} deleted successfully.");
                return (true, "Team deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting team with ID {request.TeamId}");
                return (false, "An error occurred while deleting the team.");
            }
        }
    }
}
