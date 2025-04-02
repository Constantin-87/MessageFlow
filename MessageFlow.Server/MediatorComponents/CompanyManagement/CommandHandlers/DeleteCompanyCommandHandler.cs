using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using Microsoft.Extensions.Logging;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;
using System.Net.Http.Headers;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeleteCompanyCommandHandler> _logger;
        private readonly IMediator _mediator;

        public DeleteCompanyCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            ILogger<DeleteCompanyCommandHandler> logger,
            IMediator mediator)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _httpClient = httpClientFactory.CreateClient("IdentityAPI");
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

                var token = _authorizationHelper.GetBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
                }

                var response = await _httpClient.DeleteAsync($"api/user-management/delete-company-users/{request.CompanyId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete users for company {request.CompanyId} via Identity API.");
                    return (false, "Failed to delete users for this company.");
                }

                await _mediator.Send(new DeleteTeamsByCompanyCommand(request.CompanyId));

                await _unitOfWork.Companies.RemoveEntityAsync(company);
                await _unitOfWork.SaveChangesAsync();

                //_logger.LogInformation($"Company {company.CompanyName} and all associated data deleted successfully.");
                return (true, "Company and all associated data deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company");
                return (false, "An error occurred while deleting the company.");
            }
        }
    }
}
