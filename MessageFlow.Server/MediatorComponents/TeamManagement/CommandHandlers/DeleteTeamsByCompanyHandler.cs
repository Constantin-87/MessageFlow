using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers
{
    public class DeleteTeamsByCompanyHandler : IRequestHandler<DeleteTeamsByCompanyCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<DeleteTeamsByCompanyHandler> _logger;

        public DeleteTeamsByCompanyHandler(
            IUnitOfWork unitOfWork,
            IAuthorizationHelper auth,
            ILogger<DeleteTeamsByCompanyHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _auth = auth;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteTeamsByCompanyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, errorMessage) = await _auth.CanManageTeam(request.CompanyId);
                if (!isAuthorized)
                {
                    _logger.LogWarning($"Unauthorized access to company teams: {errorMessage}");
                    return false;
                }

                var teams = await _unitOfWork.Teams.GetTeamsByCompanyIdAsync(request.CompanyId);
                if (!teams.Any())
                    return true;

                _unitOfWork.Teams.DeleteTeams(teams);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"All teams for company {request.CompanyId} deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting teams for company {request.CompanyId}");
                return false;
            }
        }
    }
}
