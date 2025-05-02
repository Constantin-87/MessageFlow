using AutoMapper;
using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Queries;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.QueryHandlers
{
    public class GetWhatsAppSettingsHandler : IRequestHandler<GetWhatsAppSettingsQuery, WhatsAppSettingsDTO?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuthorizationHelper _auth;
        private readonly ILogger<GetWhatsAppSettingsHandler> _logger;

        public GetWhatsAppSettingsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAuthorizationHelper auth,
            ILogger<GetWhatsAppSettingsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auth = auth;
            _logger = logger;
        }

        public async Task<WhatsAppSettingsDTO?> Handle(GetWhatsAppSettingsQuery request, CancellationToken cancellationToken)
        {
            var (isAuthorized, errorMessage) = await _auth.ChannelSettingsAccess(request.CompanyId);
            if (!isAuthorized)
            {
                _logger.LogWarning("Unauthorized access to WhatsApp settings: {Error}", errorMessage);
                return null;
            }
            var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(request.CompanyId);
            return _mapper.Map<WhatsAppSettingsDTO>(settings);
        }
    }
}