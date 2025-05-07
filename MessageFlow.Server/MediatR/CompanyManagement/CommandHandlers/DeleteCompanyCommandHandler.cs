using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.TeamManagement.Commands;
using MediatR;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Server.MediatR.UserManagement.Commands;

namespace MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers
{
    public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCompanyCommandHandler> _logger;
        private readonly IMediator _mediator;

        public DeleteCompanyCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            ILogger<DeleteCompanyCommandHandler> logger,
            IMediator mediator)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<(bool success, string errorMessage)> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, _) = await _authorizationHelper.CompanyAccess(request.CompanyId);
                if (!isSuperAdmin)
                    return (false, "Only SuperAdmins can delete companies.");

                var company = await _unitOfWork.Companies.GetByIdStringAsync(request.CompanyId);
                if (company == null)
                    return (false, "Company not found.");

                var deleteUsersResult = await _mediator.Send(new DeleteUsersByCompanyCommand(request.CompanyId));
                if (!deleteUsersResult)
                {
                    _logger.LogError($"Failed to delete users for company {request.CompanyId} via internal command.");
                    return (false, "Failed to delete users for this company.");
                }

                var deleteTeamsResult = await _mediator.Send(new DeleteTeamsByCompanyCommand(request.CompanyId));
                if (!deleteTeamsResult)
                {
                    _logger.LogError("Failed to delete teams for company {CompanyId}", request.CompanyId);
                    return (false, "Failed to delete teams for this company.");
                }

                var metadataFiles = await _unitOfWork.ProcessedPretrainData.GetProcessedFilesByCompanyIdAsync(request.CompanyId);
                if (metadataFiles.Any())
                {
                    var deleteMetadataResult = await _mediator.Send(new DeleteCompanyMetadataCommand(request.CompanyId));
                    if (!deleteMetadataResult.success)
                    {
                        _logger.LogError("Failed to delete metadata for company {CompanyId}. Reason: {Error}", request.CompanyId, deleteMetadataResult.errorMessage);
                        return (false, "Failed to delete metadata for this company.");
                    }
                }

                await _unitOfWork.Companies.RemoveEntityAsync(company);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Company and all associated data deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company");
                return (false, "Failed to delete metadata for this company.");
            }
        }
    }
}