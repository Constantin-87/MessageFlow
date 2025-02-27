using System.Text.Json;

namespace MessageFlow.Shared.Interfaces
{
    public interface IMessageProcessingService
    {
        Task ProcessMessageAsync(string companyId, string senderId, string username, string messageText, string providerMessageId, string source);
        Task ProcessMessageStatusUpdateAsync(JsonElement statusElement, string platform);
    }

}
