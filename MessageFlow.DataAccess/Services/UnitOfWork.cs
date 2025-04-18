using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Repositories;

namespace MessageFlow.DataAccess.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        public ApplicationDbContext Context { get; }
        public ICompanyRepository? Companies { get; }
        public ICompanyEmailRepository CompanyEmails { get; }
        public ICompanyPhoneNumberRepository CompanyPhoneNumbers { get; }
        public ITeamRepository Teams { get; }
        public IApplicationUserRepository ApplicationUsers { get; }
        public IArchivedConversationRepository ArchivedConversations { get; }
        public IConversationRepository Conversations { get; }
        public IMessageRepository Messages { get; }
        public IFacebookSettingsRepository FacebookSettings { get; }
        public IWhatsAppSettingsRepository WhatsAppSettings { get; }
        public IProcessedPretrainDataRepository ProcessedPretrainData { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            Context = context;

            if (Context == null)
                throw new ArgumentException("ApplicationDbContext is missing.");

            ApplicationUsers = new ApplicationUserRepository(Context);
            Companies = new CompanyRepository(Context);
            CompanyEmails = new CompanyEmailRepository(Context);
            CompanyPhoneNumbers = new CompanyPhoneNumberRepository(Context);
            Teams = new TeamRepository(Context);
            ArchivedConversations = new ArchivedConversationRepository(Context);
            Conversations = new ConversationRepository(Context);
            Messages = new MessageRepository(Context);
            FacebookSettings = new FacebookSettingsRepository(Context);
            WhatsAppSettings =  new WhatsAppSettingsRepository(Context);
            ProcessedPretrainData = new ProcessedPretrainDataRepository(Context);
        }

        public async Task SaveChangesAsync()
        {
            await Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
