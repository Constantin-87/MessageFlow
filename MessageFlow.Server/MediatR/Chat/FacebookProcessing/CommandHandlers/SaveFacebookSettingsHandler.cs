using AutoMapper;
using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers
{
    public class SaveFacebookSettingsHandler : IRequestHandler<SaveFacebookSettingsCommand, (bool success, string errorMessage)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<SaveFacebookSettingsHandler> _logger;
        private readonly IAuthorizationHelper _auth;

        public SaveFacebookSettingsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<SaveFacebookSettingsHandler> logger,
            IAuthorizationHelper auth)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auth = auth;
        }

        public async Task<(bool success, string errorMessage)> Handle(SaveFacebookSettingsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CompanyId))
                    return (false, "Invalid CompanyId provided.");

                var (isAuthorized, errorMessage) = await _auth.ChannelSettingsAccess(request.CompanyId);
                if (!isAuthorized)
                {
                    _logger.LogWarning("Unauthorized attempt to save Facebook settings: {Error}", errorMessage);
                    return (false, errorMessage);
                }

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
                return (true, "Facebook settings saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Facebook settings for CompanyId: {CompanyId}", request.CompanyId);
                return (false, "An error occurred while saving Facebook settings.");
            }
        }
    }
}