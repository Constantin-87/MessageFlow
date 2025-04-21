using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetConversationByIdAsync(string conversationId);
        Task<List<Conversation>> GetAssignedConversationsAsync(string userId, string companyId);
        Task<List<Conversation>> GetUnassignedConversationsAsync(string companyId);
        Task AddEntityAsync(Conversation conversation);
        Task UpdateEntityAsync(Conversation conversation);
        Task RemoveEntityAsync(Conversation conversation);
        Task<Conversation?> GetConversationBySenderIdAsync(string senderId);
        Task<Conversation?> GetConversationBySenderAndCompanyAsync(string senderId, string companyId);
    }
}