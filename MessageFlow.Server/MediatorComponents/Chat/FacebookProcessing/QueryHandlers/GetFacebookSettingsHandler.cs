using MediatR;
using AutoMapper;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Queries;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.QueryHandlers
{
    public class GetFacebookSettingsHandler : IRequestHandler<GetFacebookSettingsQuery, FacebookSettingsDTO?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetFacebookSettingsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FacebookSettingsDTO?> Handle(GetFacebookSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = await _unitOfWork.FacebookSettings.GetSettingsByCompanyIdAsync(request.CompanyId);
            return _mapper.Map<FacebookSettingsDTO>(settings);
        }
    }
}
