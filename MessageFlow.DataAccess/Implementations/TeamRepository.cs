using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;

namespace MessageFlow.DataAccess.Implementations
{
    public class TeamRepository : GenericRepository<Team>, ITeamRepository
    {
        private readonly ApplicationDbContext? _context;
        private readonly IDbContextFactoryService? _dbContextFactory;

        // ✅ Constructor for direct context usage (single-context scenarios with UnitOfWork)
        public TeamRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Constructor for factory usage (parallel-safe operations)
        //public TeamRepository(IDbContextFactoryService dbContextFactory) : base(dbContextFactory)
        //{
        //    _dbContextFactory = dbContextFactory;
        //}

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            return await _context.Teams
                .Include(t => t.Company)
                .Include(t => t.Users) // Include Users directly
                .ToListAsync();
        }

        public async Task<Team?> GetTeamByIdAsync(string teamId)
        {
            return await _context.Teams
                .Include(t => t.Company)
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == teamId);
        }

        public async Task<List<Team>> GetTeamsByCompanyIdAsync(string companyId)
        {
            return await _context.Teams
                .Where(t => t.CompanyId == companyId)
                .Include(t => t.Users)
                .ToListAsync();
        }

        public async Task<List<Team>> GetTeamsByUserIdAsync(string userId) // ✅ Get teams for a user
        {
            return await _context.Teams
                .Where(t => t.Users.Any(u => u.Id == userId))
                .Include(t => t.Users)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetUsersByTeamIdAsync(string teamId) // ✅ Get users in a team
        {
            var team = await _context.Teams
               .Include(t => t.Users)
               .FirstOrDefaultAsync(t => t.Id == teamId);

            return team?.Users.ToList() ?? new List<ApplicationUser>();
        }

        public void DeleteTeams(List<Team> teams) // ✅ Bulk delete teams
        {
            _context.Teams.RemoveRange(teams);
        }

        public async Task RemoveUserFromAllTeamsAsync(string userId)
        {
            var teamsWithUser = await GetTeamsByUserIdAsync(userId);

            if (teamsWithUser.Any())
            {
                foreach (var team in teamsWithUser)
                {
                    var userToRemove = team.Users.FirstOrDefault(u => u.Id == userId);
                    if (userToRemove != null)
                    {
                        team.Users.Remove(userToRemove);
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task AddUserToTeamAsync(string teamId, string userId)
        {
            var team = await GetTeamByIdAsync(teamId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (team != null && user != null && !team.Users.Contains(user))
            {
                team.Users.Add(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveUserFromTeamAsync(string teamId, string userId)
        {
            var team = await GetTeamByIdAsync(teamId);
            var userToRemove = team?.Users.FirstOrDefault(u => u.Id == userId);

            if (userToRemove != null)
            {
                team.Users.Remove(userToRemove);
                await _context.SaveChangesAsync();
            }
        }
    }
}
