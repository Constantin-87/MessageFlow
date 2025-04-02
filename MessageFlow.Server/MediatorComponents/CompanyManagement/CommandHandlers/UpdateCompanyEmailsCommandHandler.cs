using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class UpdateCompanyEmailsCommandHandler : IRequestHandler<UpdateCompanyEmailsCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateCompanyEmailsCommandHandler> _logger;

        public UpdateCompanyEmailsCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateCompanyEmailsCommandHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(UpdateCompanyEmailsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.CompanyEmails == null || !request.CompanyEmails.Any())
                    return (false, "No emails provided for update.");

                var companyId = request.CompanyEmails.First().CompanyId;
                if (string.IsNullOrEmpty(companyId))
                    return (false, "Invalid CompanyId provided.");

                var (isAuthorized, _, _, errorMessage) = await _authorizationHelper.CompanyAccess(companyId);
                if (!isAuthorized)
                    return (false, errorMessage);

                var existingCompany = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (existingCompany == null)
                    return (false, "Company not found.");

                var companyEmails = _mapper.Map<List<CompanyEmail>>(request.CompanyEmails);
                await _unitOfWork.CompanyEmails.UpdateEmailsAsync(companyId, companyEmails);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Company emails updated for Company id: {companyId}.");
                return (true, "Company emails updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company emails");
                return (false, "An error occurred while updating company emails.");
            }
        }
    }
}
