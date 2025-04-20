using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.CommandHandlers
{
    public class SaveWhatsAppCoreSettingsHandler : IRequestHandler<SaveWhatsAppCoreSettingsCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaveWhatsAppCoreSettingsHandler> _logger;

        public SaveWhatsAppCoreSettingsHandler(IUnitOfWork unitOfWork, ILogger<SaveWhatsAppCoreSettingsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(SaveWhatsAppCoreSettingsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CompanyId))
                    return (false, "Invalid CompanyId provided.");

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
                _logger.LogInformation("Core WhatsApp settings saved for CompanyId: {CompanyId}", request.CompanyId);
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
