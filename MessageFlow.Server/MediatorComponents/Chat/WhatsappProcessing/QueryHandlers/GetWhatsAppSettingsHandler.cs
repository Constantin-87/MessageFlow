using AutoMapper;
using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Queries;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.QueryHandlers
{
    public class GetWhatsAppSettingsHandler : IRequestHandler<GetWhatsAppSettingsQuery, WhatsAppSettingsDTO?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetWhatsAppSettingsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<WhatsAppSettingsDTO?> Handle(GetWhatsAppSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = await _unitOfWork.WhatsAppSettings.GetSettingsByCompanyIdAsync(request.CompanyId);
            return _mapper.Map<WhatsAppSettingsDTO>(settings);
        }
    }
}
