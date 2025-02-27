using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public MessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public MessageRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<Message?> GetMessageByIdAsync(string messageId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<List<Message>> GetMessagesByConversationAsync(string conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesByConversationIdAsync(string conversationId, int limit)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt) // Get the most recent messages first
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Message>> GetUnreadMessagesBeforeTimestampAsync(string conversationId, DateTime timestamp)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.Status != "read" && m.SentAt <= timestamp)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageByProviderIdAsync(string providerMessageId) // ✅ Implemented missing method
        {
            return await _context.Messages
                .FirstOrDefaultAsync(m => m.ProviderMessageId == providerMessageId);
        }

    }
}
