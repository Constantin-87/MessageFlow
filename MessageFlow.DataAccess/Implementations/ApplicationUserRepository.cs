using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.Identity;

namespace MessageFlow.DataAccess.Implementations
{
    public class ApplicationUserRepository : GenericRepository<ApplicationUser>, IApplicationUserRepository
    {
        private readonly ApplicationDbContext? _context;
        //private readonly IDbContextFactoryService? _dbContextFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;

        // ✅ Constructor for ApplicationDbContext usage (for UnitOfWork)
        public ApplicationUserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserStore<ApplicationUser> userStore)
            : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        }

        // ✅ Constructor for factory-based usage (optional, if needed by your UnitOfWork)
        //public ApplicationUserRepository(IDbContextFactoryService dbContextFactory, UserManager<ApplicationUser> userManager, IUserStore<ApplicationUser> userStore)
        //    : base(dbContextFactory)
        //{
        //    _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        //    _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        //}

        public async Task<(bool success, string errorMessage)> CreateUserAsync(ApplicationUser user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errorMsg);
            }

            return (true, "User created successfully");
        }

        public async Task<(bool success, string errorMessage)> UpdateEmailAsync(ApplicationUser user, string newEmail)
        {
            var emailStore = (IUserEmailStore<ApplicationUser>)_userStore;
            await emailStore.SetEmailAsync(user, newEmail, CancellationToken.None);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errorMsg);
            }

            return (true, "Email updated successfully");
        }

        // ✅ Fetch all role names
        public async Task<List<string>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Select(r => r.Name)
                .ToListAsync() ?? new List<string>();
        }

        public async Task<(bool success, string errorMessage)> AssignRoleAsync(ApplicationUser user, string role)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                var errorMsg = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return (false, errorMsg);
            }

            return (true, "Role assigned successfully");
        }

        // ✅ Remove all roles
        public async Task<(bool success, string errorMessage)> RemoveUserRolesAsync(ApplicationUser user)
        {
            var currentRoles = await GetRoleForUserAsync(user.Id);
            if (!currentRoles.Any())
                return (true, "No roles to remove.");

            var result = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!result.Succeeded)
            {
                var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errorMsg);
            }

            return (true, "Roles removed successfully");
        }

        // ✅ Update password
        public async Task<(bool success, string errorMessage)> UpdatePasswordAsync(ApplicationUser user, string newPassword)
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!passwordResult.Succeeded)
            {
                var errorMsg = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                return (false, errorMsg);
            }

            return (true, "Password updated successfully");
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

        public async Task<List<string>> GetRoleForUserAsync(string userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();
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

        public async Task<int> CountUsersByCompanyAsync(string companyId)
        {
            return await _context.Users.CountAsync(u => u.CompanyId == companyId);
        }


    }
}
