using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Commands.Chat
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
