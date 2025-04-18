using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Repositories
{
    public interface IArchivedConversationRepository
    {
        Task AddEntityAsync(ArchivedConversation conversation);
    }
}