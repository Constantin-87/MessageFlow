using MediatR;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.CommandHandlers
{
    public class SaveWhatsAppPhoneNumbersHandler : IRequestHandler<SaveWhatsAppPhoneNumbersCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaveWhatsAppPhoneNumbersHandler> _logger;

        public SaveWhatsAppPhoneNumbersHandler(IUnitOfWork unitOfWork, ILogger<SaveWhatsAppPhoneNumbersHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(bool success, string errorMessage)> Handle(SaveWhatsAppPhoneNumbersCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var companyId = request.PhoneNumbers.FirstOrDefault()?.CompanyId;
                if (string.IsNullOrEmpty(companyId))
                    return (false, "Invalid CompanyId provided.");

                var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(companyId);
                if (settings == null)
                    return (false, "WhatsApp settings not found.");

                settings.PhoneNumbers = request.PhoneNumbers.Select(dto => new PhoneNumberInfo
                {
                    Id = dto.Id,
                    PhoneNumber = dto.PhoneNumber,
                    PhoneNumberId = dto.PhoneNumberId,
                    PhoneNumberDesc = dto.PhoneNumberDesc,
                    WhatsAppSettingsModelId = settings.Id
                }).ToList();

                await _unitOfWork.WhatsAppSettings.UpdateEntityAsync(settings);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Phone numbers saved for CompanyId: {companyId}.");
                return (true, "Phone numbers saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving WhatsApp phone numbers.");
                return (false, "An error occurred while saving phone numbers.");
            }
        }
    }
}
