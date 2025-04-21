using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers
{
    public class UpdateCompanyPhoneNumbersCommandHandler : IRequestHandler<UpdateCompanyPhoneNumbersCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateCompanyPhoneNumbersCommandHandler> _logger;

        public UpdateCompanyPhoneNumbersCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateCompanyPhoneNumbersCommandHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(UpdateCompanyPhoneNumbersCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.CompanyPhoneNumbers == null || !request.CompanyPhoneNumbers.Any())
                    return (false, "No phone numbers provided for update.");

                var companyId = request.CompanyPhoneNumbers.First().CompanyId;
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

                var phoneNumbers = _mapper.Map<List<CompanyPhoneNumber>>(request.CompanyPhoneNumbers);
                await _unitOfWork.CompanyPhoneNumbers.UpdatePhoneNumbersAsync(companyId, phoneNumbers);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Company phone numbers updated for Company id: {companyId}.");
                return (true, "Company phone numbers updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company phone numbers");
                return (false, "An error occurred while updating company phone numbers.");
            }
        }
    }
}
