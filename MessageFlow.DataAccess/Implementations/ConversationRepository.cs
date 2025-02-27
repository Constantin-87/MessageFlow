using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public ConversationRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<List<Conversation>> GetAllConversationsAsync()
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .ToListAsync();
        }

        public async Task<Conversation?> GetConversationByIdAsync(string conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<List<Conversation>> GetConversationsByCompanyAsync(string companyId)
        {
            return await _context.Conversations
                .Where(c => c.CompanyId == companyId)
                .Include(c => c.Messages)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetActiveConversationsAsync()
        {
            return await _context.Conversations
                .Where(c => c.IsActive)
                .Include(c => c.Messages)
                .ToListAsync();
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
