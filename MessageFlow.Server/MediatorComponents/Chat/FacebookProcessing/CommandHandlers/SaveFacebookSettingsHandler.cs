using AutoMapper;
using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.CommandHandlers
{
    public class SaveFacebookSettingsHandler : IRequestHandler<SaveFacebookSettingsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveFacebookSettingsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> Handle(SaveFacebookSettingsCommand request, CancellationToken cancellationToken)
        {
            var existingSettings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(request.CompanyId);

            if (existingSettings == null)
            {
                var newSettings = _mapper.Map<FacebookSettingsModel>(request.FacebookSettingsDto);
                newSettings.CompanyId = request.CompanyId;
                await _unitOfWork.FacebookSettings.AddEntityAsync(newSettings);
            }
            else
            {
                existingSettings.PageId = request.FacebookSettingsDto.PageId;
                existingSettings.AccessToken = request.FacebookSettingsDto.AccessToken;
                await _unitOfWork.FacebookSettings.UpdateEntityAsync(existingSettings);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
