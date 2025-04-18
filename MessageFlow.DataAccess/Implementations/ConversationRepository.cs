using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        private readonly ApplicationDbContext? _context;

        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetConversationByIdAsync(string conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<List<Conversation>> GetAssignedConversationsAsync(string userId, string companyId)
        {
            return await _context.Conversations
                .Where(c => c.AssignedUserId == userId && c.CompanyId == companyId)
                .Include(c => c.Messages)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetUnassignedConversationsAsync(string companyId)
        {
            return await _context.Conversations
                .Where(c => c.CompanyId == companyId && !c.IsAssigned)
                .Include(c => c.Messages)
                .ToListAsync();
        }
        public async Task<Conversation?> GetConversationBySenderIdAsync(string senderId)
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.SenderId == senderId);
        }
        public async Task<Conversation?> GetConversationBySenderAndCompanyAsync(string senderId, string companyId)
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.SenderId == senderId && c.CompanyId == companyId);
        }
    }
}
