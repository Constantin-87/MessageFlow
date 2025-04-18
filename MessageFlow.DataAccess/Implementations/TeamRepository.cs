using MessageFlow.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Configurations;

namespace MessageFlow.DataAccess.Implementations
{
    public class TeamRepository : GenericRepository<Team>, ITeamRepository
    {
        private readonly ApplicationDbContext? _context;

        public TeamRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            return await _context.Teams
                .Include(t => t.Company)
                .Include(t => t.Users)
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

        public async Task<List<Team>> GetTeamsByUserIdAsync(string userId)
        {
            return await _context.Teams
                .Where(t => t.Users.Any(u => u.Id == userId))
                .Include(t => t.Users)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetUsersByTeamIdAsync(string teamId)
        {
            var team = await _context.Teams
               .Include(t => t.Users)
               .FirstOrDefaultAsync(t => t.Id == teamId);

            return team?.Users.ToList() ?? new List<ApplicationUser>();
        }

        public void DeleteTeams(List<Team> teams)
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
