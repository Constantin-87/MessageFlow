using MessageFlow.DataAccess.Repositories;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class ArchivedConversationRepository : GenericRepository<ArchivedConversation>, IArchivedConversationRepository
    {
        public ArchivedConversationRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}