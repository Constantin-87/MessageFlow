using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.Chat.Commands
{
    public class SendWhatsAppMessageCommand : IRequest<bool>
    {
        public string RecipientPhoneNumber { get; }
        public string MessageText { get; }
        public string CompanyId { get; }
        public string LocalMessageId { get; }

        public SendWhatsAppMessageCommand(string recipientPhoneNumber, string messageText, string companyId, string localMessageId)
        {
            RecipientPhoneNumber = recipientPhoneNumber;
            MessageText = messageText;
            CompanyId = companyId;
            LocalMessageId = localMessageId;
        }
    }
}
