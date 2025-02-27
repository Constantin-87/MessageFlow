using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IMessageRepository
    {
        Task<Message?> GetMessageByIdAsync(string messageId);
        Task<List<Message>> GetMessagesByConversationAsync(string conversationId);
        Task<List<Message>> GetAllAsync();
        Task AddEntityAsync(Message message);
        Task UpdateEntityAsync(Message message);
        Task RemoveEntityAsync(Message message);
        Task<List<Message>> GetMessagesByConversationIdAsync(string conversationId, int limit);
        Task<List<Message>> GetUnreadMessagesBeforeTimestampAsync(string conversationId, DateTime timestamp);
        Task<Message?> GetMessageByProviderIdAsync(string providerMessageId);
    }
}
