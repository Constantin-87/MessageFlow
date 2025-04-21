namespace MessageFlow.Server.Configuration
{
    public class GlobalChannelSettings
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string FacebookWebhookVerifyToken { get; set; }
        public string WhatsAppWebhookVerifyToken { get; set; }
    }
}