using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace MessageFlow.DataAccess.Services
{
    public class UnitOfWork : IUnitOfWork
    {

        //private readonly IDbContextFactoryService? _dbContextFactory;

        //private readonly UserManager<ApplicationUser> _userManager;
        //private readonly IUserStore<ApplicationUser> _userStore;

        public ApplicationDbContext Context { get; }
        public ICompanyRepository? Companies { get; }
        public ICompanyEmailRepository CompanyEmails { get; }
        public ICompanyPhoneNumberRepository CompanyPhoneNumbers { get; }
        public ITeamRepository Teams { get; }
        public IApplicationUserRepository ApplicationUsers { get; }
        public IArchivedConversationRepository ArchivedConversations { get; }
        public IArchivedMessageRepository ArchivedMessages { get; }
        public IConversationRepository Conversations { get; }
        public IMessageRepository Messages { get; }
        public IFacebookSettingsRepository FacebookSettings { get; }
        public IWhatsAppSettingsRepository WhatsAppSettings { get; }
        public IPhoneNumberInfoRepository PhoneNumbers { get; }
        public IPretrainDataFileRepository PretrainDataFiles { get; }
        public IProcessedPretrainDataRepository ProcessedPretrainData { get; }

        // ✅ Constructor with optional parameters
        public UnitOfWork(
            //UserManager<ApplicationUser> userManager,
            //IUserStore<ApplicationUser> userStore,
            ApplicationDbContext context
            /*IDbContextFactoryService? dbContextFactory = null*/)
        {
            Context = context;
            //_dbContextFactory = dbContextFactory;
            //_userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            //_userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));


            if (Context == null /*&& _dbContextFactory == null*/)
                throw new ArgumentException("❌ You must provide either ApplicationDbContext or IDbContextFactoryService.");

            ApplicationUsers = new ApplicationUserRepository(Context);
            Companies = new CompanyRepository(Context);
            CompanyEmails = new CompanyEmailRepository(Context);
            CompanyPhoneNumbers = new CompanyPhoneNumberRepository(Context);
            Teams = new TeamRepository(Context);
            ArchivedConversations = new ArchivedConversationRepository(Context);
            ArchivedMessages = new ArchivedMessageRepository(Context);
            Conversations = new ConversationRepository(Context);
            Messages = new MessageRepository(Context);
            FacebookSettings = new FacebookSettingsRepository(Context);
            WhatsAppSettings =  new WhatsAppSettingsRepository(Context);
            PhoneNumbers = new PhoneNumberInfoRepository(Context);
            PretrainDataFiles = new PretrainDataFileRepository(Context);
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
