using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class ArchivedMessageRepository : GenericRepository<ArchivedMessage>, IArchivedMessageRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public ArchivedMessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        //// ✅ Constructor for factory usage (parallel-safe operations)
        //public ArchivedMessageRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}


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
