using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers
{
    public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, (bool success, string errorMessage)>
    {
        private readonly IAuthorizationHelper _authorizationHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateCompanyCommandHandler> _logger;

        public CreateCompanyCommandHandler(
            IAuthorizationHelper authorizationHelper,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateCompanyCommandHandler> logger)
        {
            _authorizationHelper = authorizationHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var (isAuthorized, _, isSuperAdmin, _) = await _authorizationHelper.CompanyAccess(string.Empty);
                if (!isSuperAdmin)
                {
                    return (false, "Only SuperAdmins can create companies.");
                }

                var company = _mapper.Map<Company>(request.CompanyDto);
                company.Id ??= Guid.NewGuid().ToString();
                company.CompanyEmails = null;
                company.CompanyPhoneNumbers = null;
                company.Users = null;
                company.Teams = null;

                await _unitOfWork.Companies.AddEntityAsync(company);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Company created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return (false, "An error occurred while creating the company.");
            }
        }
    }
}
