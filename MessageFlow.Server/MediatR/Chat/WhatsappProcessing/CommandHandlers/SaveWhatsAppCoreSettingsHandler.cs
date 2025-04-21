using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers
{
    public class SaveWhatsAppCoreSettingsHandler : IRequestHandler<SaveWhatsAppCoreSettingsCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaveWhatsAppCoreSettingsHandler> _logger;
        private readonly IAuthorizationHelper _auth;

        public SaveWhatsAppCoreSettingsHandler(
            IUnitOfWork unitOfWork,
            ILogger<SaveWhatsAppCoreSettingsHandler> logger,
            IAuthorizationHelper auth)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _auth = auth;
        }

        public async Task<(bool success, string errorMessage)> Handle(SaveWhatsAppCoreSettingsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CompanyId))
                    return (false, "Invalid CompanyId provided.");

                var (isAuthorized, errorMessage) = await _auth.ChannelSettingsAccess(request.CompanyId);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized attempt to save WhatsApp settings: {Error}", errorMessage);
                    return (false, errorMessage);
                }

                var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(request.CompanyId);

                if (settings == null)
                {
                    settings = new WhatsAppSettingsModel
                    {
                        CompanyId = request.CompanyId,
                        BusinessAccountId = request.BusinessAccountId,
                        AccessToken = request.AccessToken
                    };
                    await _unitOfWork.WhatsAppSettings.AddEntityAsync(settings);
                }
                else
                {
                    settings.BusinessAccountId = request.BusinessAccountId;
                    settings.AccessToken = request.AccessToken;
                    await _unitOfWork.WhatsAppSettings.UpdateEntityAsync(settings);
                }

                await _unitOfWork.SaveChangesAsync();
                return (true, "Core WhatsApp settings saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving core WhatsApp settings.");
                return (false, "An error occurred while saving core WhatsApp settings.");
            }
        }
    }
}
