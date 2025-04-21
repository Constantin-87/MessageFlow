using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IMessageRepository
    {
        Task<Message?> GetMessageByIdAsync(string messageId);
        Task AddEntityAsync(Message message);
        Task UpdateEntityAsync(Message message);
        Task<List<Message>> GetMessagesByConversationIdAsync(string conversationId, int limit);
        Task<List<Message>> GetUnreadMessagesBeforeTimestampAsync(string conversationId, DateTime timestamp);
        Task<Message?> GetMessageByProviderIdAsync(string providerMessageId);
    }
}
