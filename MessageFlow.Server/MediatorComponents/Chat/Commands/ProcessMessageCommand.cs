using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.Chat.Commands
{
    public class ProcessMessageCommand : IRequest<bool>
    {
        public string CompanyId { get; }
        public string SenderId { get; }
        public string Username { get; }
        public string MessageText { get; }
        public string ProviderMessageId { get; }
        public string Source { get; }

        public ProcessMessageCommand(string companyId, string senderId, string username, string messageText, string providerMessageId, string source)
        {
            CompanyId = companyId;
            SenderId = senderId;
            Username = username;
            MessageText = messageText;
            ProviderMessageId = providerMessageId;
            Source = source;
        }
    }
}
