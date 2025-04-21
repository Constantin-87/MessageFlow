using MediatR;
using AutoMapper;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Queries;
using MessageFlow.Server.Authorization;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.QueryHandlers
{
    public class GetFacebookSettingsHandler : IRequestHandler<GetFacebookSettingsQuery, FacebookSettingsDTO?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<GetFacebookSettingsHandler> _logger;

        public GetFacebookSettingsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAuthorizationHelper auth,
            ILogger<GetFacebookSettingsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auth = auth;
            _logger = logger;
        }

        public async Task<FacebookSettingsDTO?> Handle(GetFacebookSettingsQuery request, CancellationToken cancellationToken)
        {
            var (isAuthorized, errorMessage) = await _auth.ChannelSettingsAccess(request.CompanyId);
            if (!isAuthorized)
            {
                _logger.LogWarning("Unauthorized access to Facebook settings: {Error}", errorMessage);
                return null;
            }
            var settings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(request.CompanyId);
            return _mapper.Map<FacebookSettingsDTO>(settings);
        }
    }
}
