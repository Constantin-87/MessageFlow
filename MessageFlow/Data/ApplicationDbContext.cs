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
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ArchivedConversation> ArchivedConversations { get; set; }
        public DbSet<ArchivedMessage> ArchivedMessages { get; set; }
        public DbSet<WhatsAppSettingsModel> WhatsAppSettingsModels { get; set; }
        public DbSet<PhoneNumberInfo> PhoneNumberInfo { get; set; }
        public DbSet<PretrainDataFile> PretrainDataFiles { get; set; }
        public DbSet<CompanyEmail> CompanyEmails { get; set; }
        public DbSet<CompanyPhoneNumber> CompanyPhoneNumbers { get; set; }
        public DbSet<ProcessedPretrainData> ProcessedPretrainData { get; set; }




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

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WhatsAppSettingsModel>()
                .HasMany(ws => ws.PhoneNumbers)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PretrainDataFile>()
                .HasOne(pf => pf.Company)
                .WithMany(c => c.PretrainDataFiles)
                .HasForeignKey(pf => pf.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanyEmail>()
                .HasOne(ce => ce.Company)
                .WithMany(c => c.CompanyEmails)
                .HasForeignKey(ce => ce.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanyPhoneNumber>()
                .HasOne(cp => cp.Company)
                .WithMany(c => c.CompanyPhoneNumbers)
                .HasForeignKey(cp => cp.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }

}
