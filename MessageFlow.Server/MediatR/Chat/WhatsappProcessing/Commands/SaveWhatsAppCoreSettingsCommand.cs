using MediatR;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public class SaveWhatsAppCoreSettingsCommand : IRequest<(bool success, string errorMessage)>
    {
        public string CompanyId { get; set; } = string.Empty;
        public string BusinessAccountId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }
}