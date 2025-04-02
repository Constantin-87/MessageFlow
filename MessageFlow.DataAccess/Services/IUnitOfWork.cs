using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Repositories;

namespace MessageFlow.DataAccess.Services
{
    public interface IUnitOfWork : IDisposable
    {
        public ApplicationDbContext Context { get; }
        ICompanyRepository Companies { get; }
        ICompanyEmailRepository CompanyEmails { get; }
        ICompanyPhoneNumberRepository CompanyPhoneNumbers { get; }
        ITeamRepository Teams { get; }
        IApplicationUserRepository ApplicationUsers { get; }
        IArchivedConversationRepository ArchivedConversations { get; }
        IArchivedMessageRepository ArchivedMessages { get; }
        IConversationRepository Conversations { get; }
        IMessageRepository Messages { get; }
        IFacebookSettingsRepository FacebookSettings { get; }
        IWhatsAppSettingsRepository WhatsAppSettings { get; }
        IPhoneNumberInfoRepository PhoneNumbers { get; }
        IPretrainDataFileRepository PretrainDataFiles { get; }
        IProcessedPretrainDataRepository ProcessedPretrainData { get; }


        Task SaveChangesAsync();
        //Task ExecuteInTransactionAsync(Func<ApplicationDbContext, Task> action);
    }
}
