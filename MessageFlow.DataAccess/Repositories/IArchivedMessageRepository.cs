using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IArchivedMessageRepository
    {
        Task<List<ArchivedMessage>> GetAllAsync();
        Task<List<ArchivedMessage>> GetArchivedMessagesByConversationIdAsync(string conversationId);
        Task<ArchivedMessage?> GetArchivedMessageByIdAsync(string messageId);
        Task AddEntityAsync(ArchivedMessage message);
        Task RemoveEntityAsync(ArchivedMessage message);
    }
}
