using AutoMapper;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers
{
    public class UpdateCompanyDetailsCommandHandler : IRequestHandler<UpdateCompanyDetailsCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateCompanyDetailsCommandHandler> _logger;

        public UpdateCompanyDetailsCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateCompanyDetailsCommandHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(UpdateCompanyDetailsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var companyId = request.CompanyDto.Id;
                if (string.IsNullOrEmpty(companyId))
                    return (false, "Invalid CompanyId provided.");

                var (isAuthorized, _, isSuperAdmin, errorMessage) = await _authorizationHelper.CompanyAccess(companyId);
                if (!isAuthorized)
                    return (false, errorMessage);

                var existingCompany = await _unitOfWork.Companies.GetByIdStringAsync(companyId);
                if (existingCompany == null)
                    return (false, "Company not found.");

                if (!isSuperAdmin)
                {
                    existingCompany.CompanyName = request.CompanyDto.CompanyName;
                    existingCompany.Description = request.CompanyDto.Description;
                    existingCompany.IndustryType = request.CompanyDto.IndustryType;
                    existingCompany.WebsiteUrl = request.CompanyDto.WebsiteUrl;
                }
                else
                {
                    _mapper.Map(request.CompanyDto, existingCompany);
                }

                await _unitOfWork.Companies.UpdateEntityAsync(existingCompany);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Company {request.CompanyDto.CompanyName} details updated successfully.");
                return (true, "Company details updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company details");
                return (false, "An error occurred while updating company details.");
            }
        }
    }
}
