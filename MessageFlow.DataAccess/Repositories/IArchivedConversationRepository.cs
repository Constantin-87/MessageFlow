using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IArchivedConversationRepository
    {
        Task<List<ArchivedConversation>> GetAllArchivedConversationsAsync();
        Task<ArchivedConversation?> GetArchivedConversationByIdAsync(string conversationId);
        Task<List<ArchivedConversation>> GetArchivedConversationsByCompanyAsync(string companyId);
        Task AddEntityAsync(ArchivedConversation conversation);
        Task RemoveEntityAsync(ArchivedConversation conversation);
    }
}
