using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class ArchivedConversationRepository : GenericRepository<ArchivedConversation>, IArchivedConversationRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public ArchivedConversationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        //// ✅ Constructor for factory usage (parallel-safe operations)
        //public ArchivedConversationRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<List<ArchivedConversation>> GetAllArchivedConversationsAsync()
        {
            return await _context.ArchivedConversations
                .Include(c => c.Messages)
                .ToListAsync();
        }

        public async Task<ArchivedConversation?> GetArchivedConversationByIdAsync(string conversationId)
        {
            return await _context.ArchivedConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<List<ArchivedConversation>> GetArchivedConversationsByCompanyAsync(string companyId)
        {
            return await _context.ArchivedConversations
                .Where(c => c.CompanyId == companyId)
                .Include(c => c.Messages)
                .ToListAsync();
        }

    }
}
