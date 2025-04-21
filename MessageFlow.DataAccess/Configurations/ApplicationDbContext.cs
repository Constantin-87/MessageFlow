using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.DataAccess.Configurations
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<FacebookSettingsModel> FacebookSettingsModels { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ArchivedConversation> ArchivedConversations { get; set; }
        public DbSet<ArchivedMessage> ArchivedMessages { get; set; }
        public DbSet<WhatsAppSettingsModel> WhatsAppSettingsModels { get; set; }
        public DbSet<PhoneNumberInfo> PhoneNumberInfo { get; set; }
        public DbSet<CompanyEmail> CompanyEmails { get; set; }
        public DbSet<CompanyPhoneNumber> CompanyPhoneNumbers { get; set; }
        public DbSet<ProcessedPretrainData> ProcessedPretrainData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Team>()
                .HasOne(t => t.Company)
                .WithMany(c => c.Teams)
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FacebookSettingsModel>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(f => f.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WhatsAppSettingsModel>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(ws => ws.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WhatsAppSettingsModel>()
                .HasMany(ws => ws.PhoneNumbers)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcessedPretrainData>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanyEmail>()
                .HasOne<Company>()
                .WithMany(c => c.CompanyEmails)
                .HasForeignKey(ce => ce.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanyPhoneNumber>()
                .HasOne<Company>()
                .WithMany(c => c.CompanyPhoneNumbers)
                .HasForeignKey(cp => cp.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhoneNumberInfo>()
                .HasOne(p => p.WhatsAppSettings)
                .WithMany(ws => ws.PhoneNumbers)
                .HasForeignKey(p => p.WhatsAppSettingsModelId)
                .HasPrincipalKey(w => w.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}