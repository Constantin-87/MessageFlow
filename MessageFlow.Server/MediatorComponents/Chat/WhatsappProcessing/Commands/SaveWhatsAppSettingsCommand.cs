using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands
{
    public record SaveWhatsAppSettingsCommand(string CompanyId, WhatsAppSettingsDTO SettingsDto) : IRequest<bool>;
}
