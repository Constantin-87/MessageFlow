namespace MessageFlow.Server.Helpers.Interfaces
{
    public interface IMessageSenderHelper
    {
        Task<HttpResponseMessage> SendMessageAsync(string url, object payload, string accessToken, ILogger logger);
    }
}