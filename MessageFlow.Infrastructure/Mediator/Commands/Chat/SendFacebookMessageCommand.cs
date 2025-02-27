using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Commands.Chat
{
    public class SendFacebookMessageCommand : IRequest<bool>
    {
        public string RecipientId { get; }
        public string MessageText { get; }
        public string CompanyId { get; }
        public string LocalMessageId { get; }

        public SendFacebookMessageCommand(string recipientId, string messageText, string companyId, string localMessageId)
        {
            RecipientId = recipientId;
            MessageText = messageText;
            CompanyId = companyId;
            LocalMessageId = localMessageId;
        }
    }
}
