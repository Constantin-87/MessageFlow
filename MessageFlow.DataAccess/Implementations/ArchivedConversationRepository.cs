using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class ArchivedConversationRepository : GenericRepository<ArchivedConversation>, IArchivedConversationRepository
    {
        private readonly ApplicationDbContext? _context;

        public ArchivedConversationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

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
