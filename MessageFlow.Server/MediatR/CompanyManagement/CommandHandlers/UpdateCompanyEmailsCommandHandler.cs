using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers
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

                var (isAuthorized, userCompanyId, isSuperAdmin, errorMessage) = await _authorizationHelper.CompanyAccess(companyId);
                if (!isAuthorized)
                    return (false, errorMessage);

                if (!isSuperAdmin && userCompanyId != companyId)
                    return (false, "Admins can only update their own company's emails.");

                var existingCompany = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (existingCompany == null)
                    return (false, "Company not found.");

                var companyEmails = _mapper.Map<List<CompanyEmail>>(request.CompanyEmails);
                await _unitOfWork.CompanyEmails.UpdateEmailsAsync(companyId, companyEmails);
                await _unitOfWork.SaveChangesAsync();

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