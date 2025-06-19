using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Repositories;
using MessageFlow.DataAccess.Services;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Server.Configuration
{
    public static class DataAccessExtensions
    {
        public static IServiceCollection AddRepositoriesAndDataAccess(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config["azure-database-connection-string"];
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Database connection string is missing.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<ICompanyEmailRepository, CompanyEmailRepository>();
            services.AddScoped<ICompanyPhoneNumberRepository, CompanyPhoneNumberRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IArchivedConversationRepository, ArchivedConversationRepository>();
            services.AddScoped<IFacebookSettingsRepository, FacebookSettingsRepository>();
            services.AddScoped<IWhatsAppSettingsRepository, WhatsAppSettingsRepository>();
            services.AddScoped<IProcessedPretrainDataRepository, ProcessedPretrainDataRepository>();
            services.AddScoped(typeof(GenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}