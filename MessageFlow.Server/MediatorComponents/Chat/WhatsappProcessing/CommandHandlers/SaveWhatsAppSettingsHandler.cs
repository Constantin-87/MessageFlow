using AutoMapper;
using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.CommandHandlers
{
    public class SaveWhatsAppSettingsHandler : IRequestHandler<SaveWhatsAppSettingsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveWhatsAppSettingsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> Handle(SaveWhatsAppSettingsCommand request, CancellationToken cancellationToken)
        {
            var existingSettings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(request.CompanyId);

            if (existingSettings == null)
            {
                var newSettings = _mapper.Map<WhatsAppSettingsModel>(request.SettingsDto);
                newSettings.CompanyId = request.CompanyId;
                await _unitOfWork.WhatsAppSettings.AddEntityAsync(newSettings);
            }
            else
            {
                existingSettings.AccessToken = request.SettingsDto.AccessToken;
                existingSettings.BusinessAccountId = request.SettingsDto.BusinessAccountId;
                existingSettings.PhoneNumbers = _mapper.Map<List<PhoneNumberInfo>>(request.SettingsDto.PhoneNumbers);

                await _unitOfWork.WhatsAppSettings.UpdateEntityAsync(existingSettings);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
