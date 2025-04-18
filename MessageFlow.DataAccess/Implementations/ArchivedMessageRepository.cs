using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class ArchivedMessageRepository : GenericRepository<ArchivedMessage>, IArchivedMessageRepository
    {
        private readonly ApplicationDbContext? _context;

        public ArchivedMessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<ArchivedMessage>> GetArchivedMessagesByConversationIdAsync(string conversationId)
        {
            return await _context.ArchivedMessages
                .Where(m => m.ArchivedConversationId == conversationId)
                .ToListAsync();
        }

        public async Task<ArchivedMessage?> GetArchivedMessageByIdAsync(string messageId)
        {
            return await _context.ArchivedMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }
    }
}
