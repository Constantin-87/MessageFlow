using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Queries
{
    public record GetWhatsAppSettingsQuery(string CompanyId) : IRequest<WhatsAppSettingsDTO?>;
}
