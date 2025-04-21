using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        private readonly ApplicationDbContext? _context;

        public MessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Message?> GetMessageByIdAsync(string messageId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<List<Message>> GetMessagesByConversationIdAsync(string conversationId, int limit)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Message>> GetUnreadMessagesBeforeTimestampAsync(string conversationId, DateTime timestamp)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.Status != "read" && m.SentAt <= timestamp)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageByProviderIdAsync(string providerMessageId)
        {
            return await _context.Messages
                .FirstOrDefaultAsync(m => m.ProviderMessageId == providerMessageId);
        }
    }
}
