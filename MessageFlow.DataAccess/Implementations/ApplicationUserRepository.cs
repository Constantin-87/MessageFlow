using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class ApplicationUserRepository : GenericRepository<ApplicationUser>, IApplicationUserRepository
    {
        private readonly ApplicationDbContext? _context;

        // ✅ Constructor for ApplicationDbContext usage (for UnitOfWork)
        public ApplicationUserRepository(ApplicationDbContext context)
            : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }



        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Teams)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<List<ApplicationUser>> GetUsersForCompanyAsync(string companyId)
        {
            return await _context.Users
                .Where(u => u.CompanyId == companyId)
                .Include(u => u.Company)
                .Include(u => u.Teams)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetUsersWithCompanyAsync(string currentUserCompanyId, bool isSuperAdmin)
        {
            var query = _context.Users.Include(u => u.Company).AsQueryable();

            if (!isSuperAdmin)
            {
                query = query.Where(u => u.CompanyId == currentUserCompanyId);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> DeleteUserByIdAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false; // User not found
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // DataAccess Layer: Fetch roles for multiple users in one query
        public async Task<Dictionary<string, List<string>>> GetRolesForUsersAsync(List<string> userIds)
        {
            return await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Name).ToList());
        }


        public async Task<string?> GetUserCompanyIdAsync(string userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.CompanyId) // Nullable to handle cases where user might not exist
                .FirstOrDefaultAsync();
        }

        public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Teams)
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<int> CountUsersByCompanyAsync(string companyId)
        {
            return await _context.Users.CountAsync(u => u.CompanyId == companyId);
        }


    }
}
