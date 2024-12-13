namespace MessageFlow.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using MessageFlow.Models;


    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<UserTeam> UserTeams { get; set; }
        public DbSet<FacebookSettingsModel> FacebookSettingsModels { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define the composite key for the UserTeam table
            modelBuilder.Entity<UserTeam>()
                .HasKey(ut => new { ut.UserId, ut.TeamId });

            // Configure the relationship between User and UserTeam
            modelBuilder.Entity<UserTeam>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTeams)
                .HasForeignKey(ut => ut.UserId);

            // Configure the relationship between Team and UserTeam
            modelBuilder.Entity<UserTeam>()
                .HasOne(ut => ut.Team)
                .WithMany(t => t.UserTeams)
                .HasForeignKey(ut => ut.TeamId);
        }
    }

}
